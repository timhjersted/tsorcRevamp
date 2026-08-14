using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>Hostile solar answer released by a Holy Shield output vortex. Damage is assigned from
    ///20% of the target's current maximum life by the shield; ai[0] locks collision to that player.</summary>
    class GigasHolyShieldBolt : ModProjectile
    {
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";
        static Asset<Effect> boltEffect;
        static Asset<Texture2D> macroNoise;
        static Asset<Texture2D> detailNoise;

        int TargetPlayer => (int)Projectile.ai[0];

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.light = 0.5f;
        }

        static void LoadAssets()
        {
            boltEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasHaloSun", AssetRequestMode.ImmediateLoad);
            macroNoise ??= ModContent.Request<Texture2D>(TextureRoot + "SmoothNoise", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
        }

        public override bool CanHitPlayer(Player target)
        {
            return target.whoAmI == TargetPlayer;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Dust flame = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), DustID.GoldFlame,
                -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f), 90, default,
                Main.rand.NextFloat(0.68f, 1.05f));
            flame.noGravity = true;
            if (Main.rand.NextBool(3))
            {
                Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight,
                    -Projectile.velocity * 0.03f, 30, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.4f, 0.7f));
                glint.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.72f, 0.56f, 0.16f);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    Main.rand.NextVector2Circular(2.5f, 2.5f), 70, default, Main.rand.NextFloat(0.65f, 1.05f));
                mote.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            Texture2D primary = macroNoise.Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousTexture = device.Textures[1];
            SamplerState previousSampler = device.SamplerStates[1];
            try
            {
                device.Textures[1] = detailNoise.Value;
                device.SamplerStates[1] = SamplerState.LinearWrap;
                Effect effect = boltEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GigasHaloSun"];
                effect.Parameters["OuterColor"].SetValue(new Color(100, 58, 4).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(250, 166, 20).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 246, 184).ToVector3());
                effect.Parameters["Opacity"].SetValue(0.94f);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Phase"].SetValue(Projectile.identity * 0.173f);
                effect.Parameters["Active"].SetValue(1f);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation,
                    primary.Size() * 0.5f, 44f / primary.Width, SpriteEffects.None, 0);
            }
            finally
            {
                device.Textures[1] = previousTexture;
                device.SamplerStates[1] = previousSampler;
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }
    }
}

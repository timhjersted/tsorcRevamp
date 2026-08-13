using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas heavenly spear: materializes in the air near the player as a converging sparkle cluster
    ///(harmless telegraph), then lances at the player's position. ai[0] = hover ticks before the dive;
    ///ai[1] = a 0-3 visual length tier used by the escalating barrage waves.
    ///The dive locks its direction at launch — repositioning during the hover dodges it.
    ///</summary>
    class GigasHeavenlySpear : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float DiveSpeed = 15f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> spearEffect;
        static Asset<Texture2D> macroNoise;
        static Asset<Texture2D> detailNoise;

        int HoverTicks => (int)Projectile.ai[0];
        int LengthTier => Math.Clamp((int)Projectile.ai[1], 0, 3);
        bool Diving => Projectile.localAI[0] > HoverTicks;

        static void LoadAssets()
        {
            spearEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasHeavenlySpear", AssetRequestMode.ImmediateLoad);
            macroNoise ??= ModContent.Request<Texture2D>(TextureRoot + "SmoothNoise", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 600;
        }

        public override bool? CanDamage()
        {
            return Diving;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (!Diving)
            {
                Projectile.velocity = Vector2.Zero;
                //Converging sparkles: light drawn inward to the forming spear
                float progress = Projectile.localAI[0] / (float)HoverTicks;
                for (int i = 0; i < 2; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = MathHelper.Lerp(38f, 6f, progress) + Main.rand.NextFloat(8f);
                    Vector2 pos = Projectile.Center + angle.ToRotationVector2() * radius;
                    int dust = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, 0f, 0, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (Projectile.Center - pos) * 0.12f;
                }
                Lighting.AddLight(Projectile.Center, 0.5f * progress, 0.45f * progress, 0.2f * progress);

                //Launch: lock onto the player's position right now
                if (Projectile.localAI[0] >= HoverTicks)
                {
                    Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                    if (target != null && !target.dead)
                    {
                        Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * DiveSpeed;
                    }
                    else
                    {
                        Projectile.velocity = Vector2.UnitY * DiveSpeed;
                    }
                    Projectile.tileCollide = true;
                    Projectile.timeLeft = 240;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.6f, Pitch = 0.4f }, Projectile.Center);
                }
                return;
            }

            //Diving: draw the lance as a short dust streak behind the head
            Projectile.rotation = Projectile.velocity.ToRotation();
            int segments = 3 + LengthTier * 2;
            for (int seg = 0; seg < segments; seg++)
            {
                Vector2 pos = Projectile.Center - Projectile.velocity * (seg * 0.6f);
                float scale = 1.5f - seg * 0.9f / Math.Max(1, segments - 1);
                int dust = Dust.NewDust(pos - new Vector2(4, 4), 8, 8, DustID.GoldFlame, 0f, 0f, 100, default, scale);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
            }
            Lighting.AddLight(Projectile.Center, 0.8f, 0.7f, 0.3f);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);
            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, vel.X, vel.Y, 80, default, 1.4f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            Texture2D primary = macroNoise.Value;
            float progress = MathHelper.Clamp(Projectile.localAI[0] / (float)HoverTicks, 0f, 1f);
            float visualLength = Diving ? 72f + LengthTier * 29f : 58f;
            float visualHeight = Diving ? 28f + LengthTier * 3.3f : 26f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = detailNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                Effect effect = spearEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GigasHeavenlySpear"];
                effect.Parameters["OuterColor"].SetValue(new Color(57, 31, 4).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(229, 142, 16).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 238, 163).ToVector3());
                effect.Parameters["Opacity"].SetValue(Diving ? 0.92f : 0.72f);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Active"].SetValue(Diving ? 1f : 0f);
                effect.Parameters["Phase"].SetValue(Projectile.whoAmI * 0.173f + LengthTier * 0.31f);
                effect.CurrentTechnique.Passes[0].Apply();

                // During flight x=.93 (the heavy head) sits on the existing 18px damage body;
                // only the short needle leads it. The longer left-side wake remains decorative.
                Vector2 origin = Diving ? new Vector2(primary.Width * 0.93f, primary.Height * 0.5f) : primary.Size() * 0.5f;
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White,
                    Diving ? Projectile.rotation : 0f, origin,
                    new Vector2(visualLength / primary.Width, visualHeight / primary.Height), SpriteEffects.None, 0);

                // The second pass restores the old preview's moving fissures and bright narrow
                // core without overloading the Reach-limited silhouette technique above.
                effect.CurrentTechnique = effect.Techniques["GigasHeavenlySpearDetails"];
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White,
                    Diving ? Projectile.rotation : 0f, origin,
                    new Vector2(visualLength / primary.Width, visualHeight / primary.Height), SpriteEffects.None, 0);
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

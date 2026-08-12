using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas grasping light: two tall slabs of golden light rise from the ground at the player's
    ///flanks and converge, clapping together at the point where the player was standing. Damage is
    ///only in the seam during the clap window — walk/roll out sideways before they meet.
    ///ai[0] = telegraph (converge) ticks. One projectile manages both hands as dust sculptures.
    ///</summary>
    class GigasLightHand : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float HandStartOffset = 100f;
        const float HandHeight = 130f;
        const float SlabWidth = 74f;
        const int ClapTicks = 10;
        const string TextureRoot = "tsorcRevamp/Textures/";

        static Asset<Effect> handEffect;
        static Asset<Texture2D> monolithTexture;
        static Asset<Texture2D> flowNoise;
        static Asset<Texture2D> crackNoise;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 50;
        bool Clapping => Projectile.localAI[0] > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 70;
            Projectile.height = 130;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TelegraphTicks + ClapTicks;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.5f }, Projectile.Center);
        }

        public override bool? CanDamage()
        {
            return Clapping;
        }

        static void LoadAssets()
        {
            handEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasLightHand", AssetRequestMode.ImmediateLoad);
            monolithTexture ??= ModContent.Request<Texture2D>(TextureRoot + "Particles/GigasConsecratedMonolith", AssetRequestMode.ImmediateLoad);
            flowNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/SmoothNoise", AssetRequestMode.ImmediateLoad);
            crackNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/Vein_02-512x512", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            float progress = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTicks, 0f, 1f);
            float rise = MathHelper.Min(1f, progress * 2f);
            float offset = MathHelper.Lerp(HandStartOffset, 12f, progress * progress);
            float bottom = Projectile.Center.Y + HandHeight / 2f;
            float opacity = Clapping ? 0.96f : 0.32f + progress * 0.42f;
            Texture2D monolith = monolithTexture.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousFlow = device.Textures[1];
            SamplerState previousFlowSampler = device.SamplerStates[1];
            Texture previousCrack = device.Textures[2];
            SamplerState previousCrackSampler = device.SamplerStates[2];
            try
            {
                device.Textures[1] = flowNoise.Value;
                device.SamplerStates[1] = SamplerState.LinearWrap;
                device.Textures[2] = crackNoise.Value;
                device.SamplerStates[2] = SamplerState.LinearWrap;
                Effect effect = handEffect.Value;
                effect.Parameters["GoldColor"].SetValue(new Color(244, 166, 26).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 246, 196).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.CurrentTechnique = effect.Techniques["GigasLightHandSlab"];
                for (int side = -1; side <= 1; side += 2)
                {
                    //The right slab mirrors the texture, so its UV-right edge is still the physical inner face.
                    effect.Parameters["InnerSide"].SetValue(1f);
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.EntitySpriteDraw(monolith, new Vector2(Projectile.Center.X + side * offset, bottom) - Main.screenPosition,
                        null, Color.White, 0f, new Vector2(monolith.Width * 0.5f, monolith.Height),
                        new Vector2(SlabWidth / monolith.Width, HandHeight * rise / monolith.Height),
                        side > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
                }

                if (Clapping)
                {
                    const float seamDrawWidth = 94f;
                    effect.CurrentTechnique = effect.Techniques["GigasLightHandSeam"];
                    effect.Parameters["DrawSize"].SetValue(new Vector2(seamDrawWidth, HandHeight));
                    effect.Parameters["SeamWidth"].SetValue(Projectile.width);
                    effect.Parameters["Opacity"].SetValue(0.92f);
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.EntitySpriteDraw(flowNoise.Value, Projectile.Center - Main.screenPosition, null, Color.White, 0f,
                        flowNoise.Value.Size() * 0.5f, new Vector2(seamDrawWidth / flowNoise.Value.Width, HandHeight / flowNoise.Value.Height),
                        SpriteEffects.None, 0);
                }
            }
            finally
            {
                device.Textures[1] = previousFlow;
                device.SamplerStates[1] = previousFlowSampler;
                device.Textures[2] = previousCrack;
                device.SamplerStates[2] = previousCrackSampler;
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (!Clapping)
            {
                //Two converging hand-slabs of light, rising from the ground line
                float progress = Projectile.localAI[0] / (float)TelegraphTicks;
                float offset = MathHelper.Lerp(HandStartOffset, 12f, progress * progress); //accelerating convergence
                float bottom = Projectile.Center.Y + HandHeight / 2f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = Projectile.Center.X + side * offset;
                    for (int i = 0; i < 3; i++)
                    {
                        //Rise the slab in with progress: dust only up to the current height
                        float y = bottom - Main.rand.NextFloat(HandHeight * MathHelper.Min(1f, progress * 2f));
                        int dust = Dust.NewDust(new Vector2(x - 5f, y), 10, 4, DustID.GoldFlame, 0f, 0f, 90, default, 1.3f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity = new Vector2(-side * 0.4f, -0.8f);
                    }
                    //"Fingers": brighter sparkles crowning the slab
                    if (Main.rand.NextBool(2))
                    {
                        float y = bottom - HandHeight * MathHelper.Min(1f, progress * 2f);
                        int sparkle = Dust.NewDust(new Vector2(x - 6f, y), 12, 6, DustID.GoldCoin, 0f, -1f, 0, default, 1f);
                        Main.dust[sparkle].noGravity = true;
                    }
                }
                Lighting.AddLight(Projectile.Center, 0.5f * progress, 0.45f * progress, 0.2f * progress);

                if (Projectile.localAI[0] >= TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = 0.3f }, Projectile.Center);
                    UsefulFunctions.ScreenShake(Projectile.Center, 5f, 12);
                }
                return;
            }

            //Clap: a single blazing seam where the hands met
            for (int i = 0; i < 10; i++)
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-14f, 14f), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 50, default, Main.rand.NextFloat(1.6f, 2.2f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-5f, -2f));
            }
            Lighting.AddLight(Projectile.Center, 1f, 0.9f, 0.4f);
        }
    }
}

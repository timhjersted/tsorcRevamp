using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Gigas
{
    ///<summary>
    ///Gigas lightning sweep: a long, low horizontal blade of golden lightning hugging the ground in
    ///front of the giant. Telegraphs with a line of sparkles along its full length for ai[0] ticks
    ///(dodge by jumping over it or rolling through), then flashes into the damaging sweep.
    ///Spawned as a fixed rectangle by the NPC; ai[1] = direction (only used for dust drift).
    ///</summary>
    class GigasSweepBeam : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public const int BeamLength = 720; //45 tiles
        public const int BeamHeight = 48;  //3 tiles — jumpable
        const int StrikeTicks = 120;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> sweepEffect;
        static Asset<Texture2D> macroNoise;
        static Asset<Texture2D> detailNoise;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 70;
        int Direction => (int)Projectile.ai[1] >= 0 ? 1 : -1;
        bool Striking => Projectile.localAI[0] > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = BeamLength;
            Projectile.height = BeamHeight;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TelegraphTicks + StrikeTicks;
        }

        public override bool? CanDamage()
        {
            return Striking;
        }

        static void LoadAssets()
        {
            sweepEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasSweepBeam", AssetRequestMode.ImmediateLoad);
            macroNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_Noise_6Yu1", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_07-512x512", AssetRequestMode.ImmediateLoad);
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (Main.dedServ)
            {
                return;
            }

            // Origin sits where the shader eruption emerges in front of Gigas.
            Vector2 burstOrigin = new Vector2(Projectile.Center.X - Direction * (BeamLength * 0.5f), Projectile.Center.Y);

            if (!Striking)
            {
                // Sparkle line along the beam path, brighter toward the strike.
                float progress = Projectile.localAI[0] / (float)TelegraphTicks;
                int count = 2 + (int)(progress * 3f);
                for (int i = 0; i < count; i++)
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(BeamLength), Projectile.position.Y + Main.rand.NextFloat(BeamHeight));
                    Dust sparkle = Dust.NewDustDirect(pos, 4, 4, DustID.GoldCoin, 0f, 0f, 0, default, 0.9f + progress * 0.4f);
                    sparkle.noGravity = true;
                    sparkle.velocity = new Vector2(Direction * 0.5f, -0.6f);
                }

                // Converging golden energy in a ~2-3 tile ring around the eruption origin before ignition.
                int gatherCount = 2 + (int)(progress * 4f);
                for (int i = 0; i < gatherCount; i++)
                {
                    Vector2 radial = Main.rand.NextVector2Unit();
                    Vector2 spawnPos = burstOrigin + radial * Main.rand.NextFloat(26f, 52f);
                    int dustType = i % 3 == 0 ? DustID.GoldCoin : DustID.GoldFlame;
                    Dust gather = Dust.NewDustPerfect(spawnPos, dustType,
                        (burstOrigin - spawnPos) * 0.08f, dustType == DustID.GoldCoin ? 0 : 75,
                        default, Main.rand.NextFloat(0.75f, 1.20f));
                    gather.noGravity = true;
                }

                Lighting.AddLight(burstOrigin, 0.8f * progress, 0.65f * progress, 0.25f * progress);

                if (Projectile.localAI[0] >= TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
                }

                return;
            }

            // Ignition pop: layered golden burst in a ~2.25-tile (36px) radius around the eruption origin.
            if (Projectile.localAI[0] == TelegraphTicks + 1)
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 radial = Main.rand.NextVector2Unit();
                    Vector2 spawnPos = burstOrigin + Main.rand.NextVector2Circular(36f, 36f);
                    Vector2 velocity = radial * Main.rand.NextFloat(1.8f, 5.2f) + new Vector2(Direction * Main.rand.NextFloat(2.0f, 5.5f), 0f);
                    Dust flame = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame, velocity, 55, default, Main.rand.NextFloat(0.85f, 1.45f));
                    flame.noGravity = true;
                }

                for (int i = 0; i < 20; i++)
                {
                    Vector2 radial = Main.rand.NextVector2Unit();
                    Vector2 spawnPos = burstOrigin + Main.rand.NextVector2Circular(32f, 32f);
                    Vector2 velocity = radial * Main.rand.NextFloat(2.2f, 5.8f) + new Vector2(Direction * Main.rand.NextFloat(1.2f, 3.8f), 0f);
                    Dust coin = Dust.NewDustPerfect(spawnPos, DustID.GoldCoin, velocity, 0, default, Main.rand.NextFloat(0.70f, 1.10f));
                    coin.noGravity = true;
                }

                for (int i = 0; i < 18; i++)
                {
                    Vector2 radial = Main.rand.NextVector2Unit();
                    Vector2 spawnPos = burstOrigin + Main.rand.NextVector2Circular(28f, 28f);
                    Vector2 velocity = radial * Main.rand.NextFloat(4.0f, 8.5f) + new Vector2(Direction * Main.rand.NextFloat(2.5f, 6.0f), 0f);
                    Dust spark = Dust.NewDustPerfect(spawnPos, DustID.AncientLight, velocity, 20, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.50f, 0.85f));
                    spark.noGravity = true;
                }
            }

            // Sustained origin eruption: dense golden flames, glints, and bright sparks within ~2.25 tiles.
            for (int i = 0; i < 9; i++)
            {
                Vector2 radial = Main.rand.NextVector2Unit();
                Vector2 spawnPos = burstOrigin + Main.rand.NextVector2Circular(36f, 36f);
                Vector2 velocity = radial * Main.rand.NextFloat(1.2f, 3.2f) + new Vector2(Direction * Main.rand.NextFloat(2.5f, 6.8f), 0f);
                Dust originFlame = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame, velocity, 55, default, Main.rand.NextFloat(0.80f, 1.40f));
                originFlame.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 radial = Main.rand.NextVector2Unit();
                Vector2 spawnPos = burstOrigin + Main.rand.NextVector2Circular(34f, 34f);
                Vector2 velocity = radial * Main.rand.NextFloat(1.4f, 3.6f) + new Vector2(Direction * Main.rand.NextFloat(1.0f, 3.2f), Main.rand.NextFloat(-1.2f, 1.2f));
                Dust originCoin = Dust.NewDustPerfect(spawnPos, DustID.GoldCoin, velocity, 0, default, Main.rand.NextFloat(0.65f, 1.05f));
                originCoin.noGravity = true;
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 radial = Main.rand.NextVector2Unit();
                Vector2 spawnPos = burstOrigin + Main.rand.NextVector2Circular(28f, 28f);
                Vector2 velocity = radial * Main.rand.NextFloat(2.5f, 5.5f) + new Vector2(Direction * Main.rand.NextFloat(3.0f, 7.5f), 0f);
                Dust originSpark = Dust.NewDustPerfect(spawnPos, DustID.AncientLight, velocity, 20, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.50f, 0.85f));
                originSpark.noGravity = true;
            }

            Lighting.AddLight(burstOrigin, 1.35f, 1.10f, 0.45f);

            // Strike: dense golden lightning filling the band
            for (int i = 0; i < 22; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(BeamLength), Projectile.position.Y + Main.rand.NextFloat(BeamHeight));
                Dust dust = Dust.NewDustDirect(pos, 4, 4, DustID.GoldFlame, Direction * 3f, 0f, 50, default, Main.rand.NextFloat(1.3f, 1.85f));
                dust.noGravity = true;
                dust.velocity = new Vector2(Direction * Main.rand.NextFloat(2f, 6f), Main.rand.NextFloat(-1f, 1f));
            }

            // White-hot core streaks
            for (int i = 0; i < 5; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(BeamLength), Projectile.Center.Y + Main.rand.NextFloat(-8f, 8f));
                Dust core = Dust.NewDustDirect(pos, 2, 2, DustID.AncientLight, Direction * 5f, 0f, 20, Color.LightGoldenrodYellow, 1.25f);
                core.noGravity = true;
            }

            for (int seg = 0; seg < 6; seg++)
            {
                Lighting.AddLight(new Vector2(Projectile.position.X + BeamLength * (seg + 0.5f) / 6f, Projectile.Center.Y), 1f, 0.9f, 0.4f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            float progress = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTicks, 0f, 1f);
            float active = Striking ? 1f : 0f;
            float fadeOut = Striking ? MathHelper.Clamp(Projectile.timeLeft / 3f, 0f, 1f) : 1f;
            float opacity = (Striking ? 0.94f : 0.58f) * fadeOut;
            Texture2D primary = macroNoise.Value;

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

                Effect effect = sweepEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GigasSweepBeam"];
                effect.Parameters["OuterColor"].SetValue(new Color(105, 61, 5).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(255, 174, 21).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 244, 183).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Active"].SetValue(active);
                effect.Parameters["Direction"].SetValue(Direction);
                effect.Parameters["DrawSize"].SetValue(new Vector2(752f, 72f));
                effect.CurrentTechnique.Passes[0].Apply();

                // The 72px visual shell permits a soft halo; the 48px gold body is kept within
                // BeamHeight so the readable attack width remains mechanically honest.
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White,
                    0f, primary.Size() * 0.5f, new Vector2(752f / primary.Width, 72f / primary.Height),
                    SpriteEffects.None, 0);
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

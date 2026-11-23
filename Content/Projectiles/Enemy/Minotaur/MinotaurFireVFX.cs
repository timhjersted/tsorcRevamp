using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Minotaur
{
    internal static class MinotaurFireVFX
    {
        static Asset<Effect> fire;
        static Asset<Texture2D> noise;

        // Compact material layer complementing the dust. The flame points up before rotation.
        internal static void Draw(Vector2 center, Vector2 size, float opacity, float seed, float rotation = 0f,
            bool firebolt = false)
        {
            if (Main.dedServ || opacity <= 0f) return;
            fire ??= ModContent.Request<Effect>("tsorcRevamp/Effects/MinotaurFire", AssetRequestMode.ImmediateLoad);
            noise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/SmoothNoise", AssetRequestMode.ImmediateLoad);
            Effect effect = fire.Value;
            effect.CurrentTechnique = effect.Techniques[firebolt ? "MinotaurFirebolt" : "MinotaurFire"];
            Vector2 blocks = Vector2.Max(size / 2f, Vector2.One);
            effect.Parameters["PixelGrid"].SetValue(new Vector4(blocks.X, blocks.Y, 1f / blocks.X, 1f / blocks.Y));
            effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["Opacity"].SetValue(MathHelper.Clamp(opacity, 0f, 1f));
            effect.Parameters["Seed"].SetValue(seed);
            Main.spriteBatch.End();
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                    DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(noise.Value, center - Main.screenPosition, null, Color.White,
                    rotation, noise.Value.Size() * 0.5f, size / noise.Value.Size(), SpriteEffects.None, 0f);
            }
            finally
            {
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        internal static void Embers(Vector2 bottom, float halfWidth, int count, float strength = 1f)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int dustType = DustID.Torch;
                if (i % 3 == 0)
                {
                    dustType = DustID.OrangeTorch;
                }

                Vector2 pos = bottom + new Vector2(Main.rand.NextFloat(-halfWidth, halfWidth), Main.rand.NextFloat(-6f, -1f));
                Vector2 vel = new Vector2(Main.rand.NextFloat(-0.75f, 0.75f), Main.rand.NextFloat(-3.4f, -1.1f));
                Dust ember = Dust.NewDustPerfect(pos, dustType, vel, 50, default, Main.rand.NextFloat(0.85f, 1.35f) * strength);
                ember.noGravity = true;
            }
        }

        /// <summary>3-layer roaring ground-fire column shared by MinotaurFloorFlame and MinotaurFlameWave:
        /// 3 midground Torch/OrangeTorch body motes (max scale 1.2 * 1.65 = 1.98), 2 fast rising sparks,
        /// and optional dark warm-tinted billowing smoke that outlives the flames.</summary>
        internal static void BurnColumn(Vector2 bottom, float halfWidth, float strength, bool spawnSmoke)
        {
            if (Main.dedServ || strength <= 0.05f)
            {
                return;
            }

            // Midground body: dense licking flames across the tile footprint.
            for (int i = 0; i < 3; i++)
            {
                int dustType = DustID.Torch;
                if (i == 0)
                {
                    dustType = DustID.OrangeTorch;
                }

                Vector2 pos = bottom + new Vector2(Main.rand.NextFloat(-halfWidth, halfWidth), Main.rand.NextFloat(-14f, -2f));
                Vector2 vel = new Vector2(Main.rand.NextFloat(-0.9f, 0.9f), Main.rand.NextFloat(-4.4f, -1.8f));
                Dust body = Dust.NewDustPerfect(pos, dustType, vel, 45, default, Main.rand.NextFloat(1.05f, 1.65f) * strength);
                body.noGravity = true;
            }

            // Foreground sparks: small, bright, fast-rising embers shooting above the flame tips.
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = bottom + new Vector2(Main.rand.NextFloat(-halfWidth * 0.85f, halfWidth * 0.85f), Main.rand.NextFloat(-18f, -4f));
                Vector2 vel = new Vector2(Main.rand.NextFloat(-1.3f, 1.3f), Main.rand.NextFloat(-6.0f, -3.2f));
                Dust spark = Dust.NewDustPerfect(pos, DustID.Torch, vel, 20, default, Main.rand.NextFloat(0.55f, 0.90f) * strength);
                spark.noGravity = true;
            }

            // Background smoke: spawns small and expands via fadeIn, tinted warm charcoal so it reads as soot.
            if (spawnSmoke)
            {
                Vector2 pos = bottom + new Vector2(Main.rand.NextFloat(-halfWidth * 0.7f, halfWidth * 0.7f), Main.rand.NextFloat(-22f, -10f));
                Vector2 vel = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.7f, -0.9f));
                Dust smoke = Dust.NewDustPerfect(pos, DustID.Smoke, vel, 160, new Color(52, 36, 30), Main.rand.NextFloat(0.55f, 0.80f));
                smoke.fadeIn = Main.rand.NextFloat(1.35f, 1.70f);
                smoke.noGravity = true;
                smoke.noLight = true;
            }
        }

        internal static void Extinguish(Vector2 center, int count)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Midground fire burst.
            for (int i = 0; i < count; i++)
            {
                int dustType = DustID.Torch;
                if (i % 3 == 0)
                {
                    dustType = DustID.OrangeTorch;
                }

                Vector2 vel = Main.rand.NextVector2Circular(3.6f, 3.6f) + new Vector2(0f, -1.6f);
                Dust body = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(6f, 6f), dustType, vel, 45, default, Main.rand.NextFloat(0.95f, 1.55f));
                body.noGravity = true;
            }

            // Foreground fast sparks.
            int sparkCount = System.Math.Max(4, count / 2);
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(5.8f, 5.8f) + new Vector2(0f, -2.2f);
                Dust spark = Dust.NewDustPerfect(center, DustID.Torch, vel, 15, default, Main.rand.NextFloat(0.50f, 0.85f));
                spark.noGravity = true;
            }

            // Background billowing dark smoke that outlives the burst.
            int smokeCount = System.Math.Max(3, count / 3);
            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(1.4f, 1.4f) + new Vector2(0f, -1.1f);
                Dust smoke = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8f, 6f), DustID.Smoke, vel, 155, new Color(52, 36, 30), Main.rand.NextFloat(0.55f, 0.80f));
                smoke.fadeIn = Main.rand.NextFloat(1.35f, 1.70f);
                smoke.noGravity = true;
                smoke.noLight = true;
            }
        }
    }
}

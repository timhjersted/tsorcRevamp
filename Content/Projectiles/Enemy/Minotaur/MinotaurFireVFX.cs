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
            if (Main.dedServ) return;
            for (int i = 0; i < count; i++)
            {
                Dust ember = Dust.NewDustPerfect(bottom + new Vector2(Main.rand.NextFloat(-halfWidth, halfWidth), -3f),
                    DustID.Torch, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-2.8f, -0.8f)),
                    100, default, Main.rand.NextFloat(0.6f, 1f) * strength);
                ember.noGravity = true;
            }
        }

        internal static void Extinguish(Vector2 center, int count)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < count; i++)
            {
                Dust ember = Dust.NewDustPerfect(center, DustID.Torch,
                    Main.rand.NextVector2Circular(3f, 3f) - Vector2.UnitY, 90, default, Main.rand.NextFloat(0.6f, 1.2f));
                ember.noGravity = true;
            }
            Dust.NewDustPerfect(center, DustID.Smoke, new Vector2(0f, -0.7f), 150, default, 0.8f);
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas sun pillar: a column of judgment light. Spawned with its bottom on the ground under the
    ///player. Telegraphs with faint rising motes for ai[0] ticks, then the beam slams down — dense
    ///golden column, damaging for the strike window only. ai[1] is a dormant start delay, letting
    ///a parent attack schedule an entire sequence and safely continue with another move.
    ///</summary>
    class GigasSunPillar : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int PillarWidth = 44;
        public const int PillarHeight = 480;
        const int StrikeTicks = 90;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> pillarEffect;
        static Asset<Texture2D> macroNoise;
        static Asset<Texture2D> detailNoise;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 45;
        int StartDelayTicks => (int)Projectile.ai[1];
        int StartedAge => (int)Projectile.localAI[0] - StartDelayTicks;
        bool Started => StartedAge > 0;
        bool Striking => Started && StartedAge > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = PillarWidth;
            Projectile.height = PillarHeight;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = StartDelayTicks + TelegraphTicks + StrikeTicks;
        }

        public override bool? CanDamage()
        {
            return Striking;
        }

        // The entire shader quad, including its inner filament, is occluded by terrain. Dust remains
        // on Terraria's normal particle layer, so only the requested light column goes behind tiles.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        static void LoadAssets()
        {
            pillarEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasSunPillar", AssetRequestMode.ImmediateLoad);
            macroNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_Noise_6Yu1", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_07-512x512", AssetRequestMode.ImmediateLoad);
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            if (!Started)
            {
                return;
            }
            float groundY = Projectile.position.Y + Projectile.height;

            if (!Striking)
            {
                //Ground marker: a bright simmering pool of light at the base
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), groundY - 10f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, -1.5f, 100, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
                //Faint motes rising through the whole column — the read for "get out of the light"
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), Projectile.position.Y + Main.rand.NextFloat(PillarHeight));
                    int mote = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, -2f, 0, default, 0.9f);
                    Main.dust[mote].noGravity = true;
                    Main.dust[mote].velocity = new Vector2(0f, -2.5f);
                }
                Lighting.AddLight(new Vector2(Projectile.Center.X, groundY - 20f), 0.5f, 0.45f, 0.15f);

                if (StartedAge >= TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.45f, Pitch = 0.3f }, new Vector2(Projectile.Center.X, groundY));
                }
                return;
            }

            //Strike: dense column of light along the full height
            for (int i = 0; i < 10; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), Projectile.position.Y + Main.rand.NextFloat(PillarHeight));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 60, default, Main.rand.NextFloat(1.5f, 2.2f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-4f, -1f));
            }
            //White-hot core
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-6f, 6f), Projectile.position.Y + Main.rand.NextFloat(PillarHeight));
                int core = Dust.NewDust(pos, 2, 2, DustID.AncientLight, 0f, 0f, 20, Color.LightGoldenrodYellow, 1.6f);
                Main.dust[core].noGravity = true;
                Main.dust[core].velocity *= 0.2f;
            }
            //Impact splash at the base
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), groundY - 8f);
                int splash = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, Main.rand.NextFloat(-3f, 3f), -2f, 80, default, 1.5f);
                Main.dust[splash].noGravity = true;
            }
            for (int seg = 0; seg < 4; seg++)
            {
                Lighting.AddLight(new Vector2(Projectile.Center.X, Projectile.position.Y + PillarHeight * (seg + 0.5f) / 4f), 1f, 0.9f, 0.4f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            if (!Started)
            {
                return false;
            }

            float progress = MathHelper.Clamp(StartedAge / (float)TelegraphTicks, 0f, 1f);
            float active = Striking ? 1f : 0f;
            float fadeOut = Striking ? MathHelper.Clamp(Projectile.timeLeft / 5f, 0f, 1f) : 1f;
            float opacity = (Striking ? 0.92f : 0.70f) * fadeOut;
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

                Effect effect = pillarEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GigasSunPillar"];
                effect.Parameters["OuterColor"].SetValue(new Color(112, 69, 8).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(255, 180, 35).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 243, 172).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Active"].SetValue(active);
                effect.Parameters["DrawSize"].SetValue(new Vector2(80f, 520f));
                effect.Parameters["PixelBlockSize"].SetValue(2f);
                effect.CurrentTechnique.Passes[0].Apply();

                // 80px shell gives the soft halo room; the shader's 44px gold body is contained in
                // PillarWidth, so the brightest broad band remains the actual damage lane.
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White,
                    0f, primary.Size() * 0.5f, new Vector2(80f / primary.Width, 520f / primary.Height),
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas solar boulder: a huge, slow lobbed sun. Arcs under gravity (ai[0] = gravity per tick),
    ///detonates on impact into bouncing solar embers and leaves consecrated ground where it lands.
    ///</summary>
    class GigasSolarBoulder : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const string TextureRoot = "tsorcRevamp/Textures/Noise/";
        static Asset<Effect> boulderEffect;
        static Asset<Texture2D> macroNoise;
        static Asset<Texture2D> detailNoise;
        static Asset<Texture2D> meteorTexture;

        static void LoadAssets()
        {
            boulderEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasSolarBoulder", AssetRequestMode.ImmediateLoad);
            macroNoise ??= ModContent.Request<Texture2D>(TextureRoot + "SmoothNoise", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
            meteorTexture ??= ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/SolarMeteor", AssetRequestMode.ImmediateLoad);
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 600;
            Projectile.light = 0.9f;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }
            Projectile.rotation += 0.06f * (Projectile.velocity.X >= 0f ? 1f : -1f);

            //Roiling golden aura — the boulder is a miniature sun
            for (int i = 0; i < 4; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 60, default, Main.rand.NextFloat(1.8f, 2.4f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
            if (Main.rand.NextBool(2))
            {
                int sparkle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, -1f, 0, default, 1.1f);
                Main.dust[sparkle].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 1f, 0.85f, 0.35f);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = 0.2f }, Projectile.Center);
            UsefulFunctions.ScreenShake(Projectile.Center, 7f, 14);

            //Detonation flash
            for (int i = 0; i < 30; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, vel.X, vel.Y, 60, default, Main.rand.NextFloat(1.5f, 2.2f));
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            //Bouncing embers fly up and out
            int embers = Main.rand.Next(4, 6);
            for (int i = 0; i < embers; i++)
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-9f, -5f));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<GigasSolarEmber>(), (int)(Projectile.damage * 0.5f), 0f, Main.myPlayer, 0.25f);
            }

            //Consecrate the ground below the impact
            int tileX = (int)(Projectile.Center.X / 16f);
            int tileY = (int)(Projectile.Center.Y / 16f);
            for (int d = 0; d < 6; d++)
            {
                int y = tileY + d;
                if (y >= Main.maxTilesY)
                {
                    break;
                }
                Tile tile = Main.tile[tileX, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    Vector2 patchCenter = new Vector2(tileX * 16f + 8f, y * 16f - GigasConsecratedGround.PatchHeight / 2f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), patchCenter, Vector2.Zero, ModContent.ProjectileType<GigasConsecratedGround>(), (int)(Projectile.damage * 0.4f), 0f, Main.myPlayer);
                    break;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            float progress = 1f - Projectile.timeLeft / 600f;
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

                Effect effect = boulderEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GigasSolarBoulder"];
                effect.Parameters["OuterColor"].SetValue(new Color(43, 23, 10).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(246, 137, 16).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 239, 166).ToVector3());
                effect.Parameters["Opacity"].SetValue(0.92f);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(MathHelper.Clamp(progress, 0f, 1f));
                effect.CurrentTechnique.Passes[0].Apply();

                // The 88px VFX shell leaves room for the irregular corona. Its dense furnace body
                // stays concentrated around the existing 36px hostile projectile; no mechanics change.
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White,
                    Projectile.rotation, primary.Size() * 0.5f, new Vector2(88f / primary.Width),
                    SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            // Solid meteor mass over the procedural furnace shell: preserve its native colour while
            // exposing the moving shader through it at exactly 80% opacity.
            Texture2D meteor = meteorTexture.Value;
            Main.EntitySpriteDraw(meteor, Projectile.Center - Main.screenPosition, null,
                new Color(255, 255, 255, 204), Projectile.rotation, meteor.Size() * 0.5f,
                1f, SpriteEffects.None, 0);
            return false;
        }
    }
}

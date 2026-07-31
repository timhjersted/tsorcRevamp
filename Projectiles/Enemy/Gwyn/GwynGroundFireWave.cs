using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>A large cinder wave that rolls away from Gwyn's Winged Plunge impact.</summary>
    class GwynGroundFireWave : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float WaveSpeed = 7f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> cinderEffect;
        static Asset<Texture2D> auraNoise;
        static Asset<Texture2D> flowNoise;

        int Direction => Projectile.ai[0] >= 0f ? 1 : -1;
        int ReachTiles => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : 20;
        int Lifetime => (int)(ReachTiles * 16f / WaveSpeed) + 5;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 64;
            Projectile.height = 58;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.light = 0.8f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.position.X += Direction * WaveSpeed;
            if (!SnapToGround())
            {
                Projectile.Kill();
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-22f, 22f), Main.rand.NextFloat(-18f, 0f));
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                Dust dust = Dust.NewDustPerfect(position, type,
                    new Vector2(Direction * Main.rand.NextFloat(0.8f, 2.2f), Main.rand.NextFloat(-4.2f, -1.6f)),
                    70, default, Main.rand.NextFloat(1.3f, 2f));
                dust.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 1f, 0.5f, 0.12f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 5 * 60);
        }

        static void LoadAssets()
        {
            cinderEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynCinderTrail", AssetRequestMode.ImmediateLoad);
            auraNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            flowNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            Rectangle source = new Rectangle(0, 0, 72, 68);
            float progress = MathHelper.Clamp(1f - Projectile.timeLeft / (float)Lifetime, 0f, 1f);
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 8f, 0.35f, 1f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = cinderEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = flowNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                effect.CurrentTechnique = effect.Techniques["GwynCinderArc"];
                effect.Parameters["CinderColor"].SetValue(new Color(255, 28, 2).ToVector3());
                effect.Parameters["FlameColor"].SetValue(new Color(255, 118, 10).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 232, 154).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(auraNoise.Value.Size());
                effect.Parameters["Progress"].SetValue(progress);
                effect.CurrentTechnique.Passes[0].Apply();

                for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    float trailOpacity = (1f - i / (float)Projectile.oldPos.Length) * 0.48f;
                    Vector2 position = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(auraNoise.Value, position, source, Color.White * trailOpacity,
                        Direction > 0 ? 0f : MathHelper.Pi, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
                }
                Main.EntitySpriteDraw(auraNoise.Value, Projectile.Center - Main.screenPosition, source, Color.White,
                    Direction > 0 ? 0f : MathHelper.Pi, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        bool SnapToGround()
        {
            int tileX = (int)((Projectile.Center.X + Direction * Projectile.width * 0.5f) / 16f);
            int tileY = (int)((Projectile.position.Y + Projectile.height - 8f) / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 10)
                return false;

            int climb = 0;
            while (IsSolid(tileX, tileY) && climb <= 3)
            {
                tileY--;
                climb++;
            }
            if (climb > 3)
                return false;

            int drop = 0;
            while (!IsSolid(tileX, tileY + 1) && drop <= 5)
            {
                tileY++;
                drop++;
            }
            if (drop > 5)
                return false;

            Projectile.position.Y = (tileY + 1) * 16f - Projectile.height;
            return true;
        }

        static bool IsSolid(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
        }
    }
}
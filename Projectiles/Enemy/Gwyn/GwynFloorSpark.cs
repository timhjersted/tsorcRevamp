using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Stage 3 of Gwyn's Spear of the First Sun: electricity racing along the floor away from the
    ///bolt strike. A compact shader core and crackling dust hug the terrain (steps up/down
    ///small ledges), dies at tall walls and pits. Jump it. ai[0] = direction (-1/1), ai[1] = tiles
    ///of reach.
    ///</summary>
    class GwynFloorSpark : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float SparkSpeed = 8f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> judgmentEffect;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;

        int Direction => (int)Projectile.ai[0] >= 0 ? 1 : -1;
        int ReachTiles => (int)Projectile.ai[1] > 0 ? (int)Projectile.ai[1] : 12;
        int Lifetime => (int)(ReachTiles * 16f / SparkSpeed) + 4;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 14;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        bool VisualOnly => Projectile.ai[2] > 0f;

        public override bool? CanDamage() => !VisualOnly;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 44;
            Projectile.height = 42;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 120;
            Projectile.light = 0.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.position.X += Direction * SparkSpeed;
            if (!SnapToGround())
            {
                Projectile.Kill();
                return;
            }

            //A restrained spark garnish; the shader trail carries the readable ground lightning.
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Projectile.height - 6f);
                int dust = Dust.NewDust(pos, 2, 2, DustID.Electric, Direction, Main.rand.NextFloat(-2.5f, -0.8f), 80,
                    new Color(255, 211, 84), Main.rand.NextFloat(0.75f, 1.05f));
                Main.dust[dust].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.7f, 0.6f, 0.25f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
        }

        static void LoadAssets()
        {
            judgmentEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynSunlightJudgment", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            Rectangle source = new Rectangle(0, 0, 52, 54);
            float progress = MathHelper.Clamp(1f - Projectile.timeLeft / (float)Lifetime, 0f, 1f);
            float fadeIn = MathHelper.Clamp(progress / 0.12f, 0.4f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 4f, 0.45f, 1f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = judgmentEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = brokenNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.CurrentTechnique = effect.Techniques["GwynSunlightJudgmentGround"];
                effect.Parameters["GoldColor"].SetValue(new Color(255, 139, 18).ToVector3());
                effect.Parameters["HotColor"].SetValue(new Color(255, 218, 78).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 252, 218).ToVector3());
                effect.Parameters["Opacity"].SetValue(fadeIn * fadeOut * (VisualOnly ? 0.72f : 1f));
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(smoothNoise.Value.Size());
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Active"].SetValue(1f);
                effect.Parameters["Direction"].SetValue((float)Direction);
                effect.CurrentTechnique.Passes[0].Apply();

                for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;

                    float trailOpacity = 1f - i / (float)Projectile.oldPos.Length;
                    Vector2 trailCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(smoothNoise.Value, trailCenter, source, Color.White * trailOpacity,
                        0f, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
                }

                Main.EntitySpriteDraw(smoothNoise.Value, Projectile.Center - Main.screenPosition, source,
                    Color.White, 0f, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        ///<summary>Aligns the spark bottom with the ground; false = blocked by a tall wall or a pit.</summary>
        bool SnapToGround()
        {
            int tileX = (int)((Projectile.Center.X + Direction * Projectile.width / 2f) / 16f);
            int tileY = (int)((Projectile.position.Y + Projectile.height - 8f) / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 10)
            {
                return false;
            }
            int climb = 0;
            while (IsSolid(tileX, tileY) && climb <= 3)
            {
                tileY--;
                climb++;
            }
            if (climb > 3)
            {
                return false;
            }
            int drop = 0;
            while (!IsSolid(tileX, tileY + 1) && drop <= 5)
            {
                tileY++;
                drop++;
            }
            if (drop > 5)
            {
                return false;
            }
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

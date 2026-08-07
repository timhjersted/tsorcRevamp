using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    /// <summary>
    /// One damaging spoke of sunlight lightning released by the player's thrown spear. Its
    /// shader and collision use the same rotated line, while shared immunity prevents the
    /// radial spokes from multiplying damage against one target.
    /// </summary>
    class SwordOfLordGwynImpactLightning : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int Lifetime = 16;
        const float BoltLength = 72f;
        const float BoltWidth = 24f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> judgmentEffect;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;

        bool Empowered => Projectile.ai[0] == 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.965f;

            if (Main.rand.NextBool(2))
            {
                Vector2 normal = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                Vector2 velocity = -Projectile.velocity * 0.08f
                    + normal * Main.rand.NextFloat(-1.25f, 1.25f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, velocity, 70,
                    new Color(255, 228, 126), Main.rand.NextFloat(0.55f, 0.85f));
                dust.noGravity = true;
            }

            float intensity = Projectile.timeLeft / (float)Lifetime;
            Lighting.AddLight(Projectile.Center, 0.9f * intensity, 0.75f * intensity, 0.28f * intensity);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - direction * BoltLength * 0.55f;
            Vector2 end = Projectile.Center + direction * BoltLength * 0.45f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                start, end, BoltWidth, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, Empowered ? 4 * 60 : 3 * 60);
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

            Rectangle source = new Rectangle(0, 0, (int)BoltLength, (int)BoltWidth);
            float lifeProgress = 1f - Projectile.timeLeft / (float)Lifetime;
            float fadeIn = MathHelper.Clamp(lifeProgress / 0.12f, 0.55f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 5f, 0f, 1f);

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
                effect.Parameters["GoldColor"].SetValue(new Color(255, 151, 24).ToVector3());
                effect.Parameters["HotColor"].SetValue(new Color(255, 224, 100).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 255, 226).ToVector3());
                effect.Parameters["Opacity"].SetValue(fadeIn * fadeOut * (Empowered ? 1f : 0.88f));
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(smoothNoise.Value.Size());
                effect.Parameters["Progress"].SetValue(lifeProgress);
                effect.Parameters["Active"].SetValue(1f);
                effect.Parameters["Direction"].SetValue(1f);
                effect.CurrentTechnique.Passes[0].Apply();

                for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                    {
                        continue;
                    }

                    float trailOpacity = (1f - i / (float)Projectile.oldPos.Length) * 0.42f;
                    Vector2 center = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(smoothNoise.Value, center, source, Color.White * trailOpacity,
                        Projectile.rotation, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
                }

                Main.EntitySpriteDraw(smoothNoise.Value, Projectile.Center - Main.screenPosition, source,
                    Color.White, Projectile.rotation, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
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

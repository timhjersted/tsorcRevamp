using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Descent of the Sun presentation reused by Gwyn's smaller impact effects. This deliberately
    /// uses the mod's established
    /// immediate/additive shader path and ordinary texture assets rather than the experimental VFX
    /// strip renderer, so it can survive that renderer being replaced. Only Winged Plunge mode
    /// owns damage; the Descent and Lord's Embrace modes remain presentation-only.
    /// ai[0] = telegraph/impact mode; ai[1] = parent meteor index while telegraphing.
    /// </summary>
    class GwynDescentColumn : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int TelegraphMode = 0;
        public const int ImpactMode = 1;
        public const int ExplosionOnlyMode = 2;
        public const int WingedPlungeExplosionMode = 3;

        const int ImpactLifetime = 68;
        const int ColumnHeight = 760;
        const int WingedPlungeDamageTicks = 8;
        const int WingedPlungeHitboxDiameter = 240;
        const float ExplosionCenterHeight = 48f;
        const int WingedPlungeBroadphaseHeight = WingedPlungeHitboxDiameter + (int)(ExplosionCenterHeight * 2f);
        const float WingedPlungeVisualDiameter = WingedPlungeHitboxDiameter * 1.25f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> solarColumnEffect;
        static Asset<Texture2D> solarFlowNoise;
        static Asset<Texture2D> flameCurtain;
        static Asset<Texture2D> fireFlipbook;
        static Asset<Texture2D> smokeFlipbook;
        static Asset<Texture2D> impactFlare;
        static Asset<Texture2D> groundCracks;
        static Asset<Texture2D> windStreak;

        bool Telegraphing => (int)Projectile.ai[0] == TelegraphMode;
        bool WingedPlungeExplosion => (int)Projectile.ai[0] == WingedPlungeExplosionMode;
        bool ExplosionOnly => (int)Projectile.ai[0] == ExplosionOnlyMode || WingedPlungeExplosion;
        int ParentIndex => (int)Projectile.ai[1];
        float Age => Projectile.localAI[0];
        Vector2 ImpactAnchor => Projectile.Center;
        Vector2 ExplosionCenter => ImpactAnchor - Vector2.UnitY * ExplosionCenterHeight;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (!Telegraphing)
                Projectile.timeLeft = ImpactLifetime;

            if (WingedPlungeExplosion)
            {
                // Keep the projectile centered on Descent's ground anchor on every peer. The taller
                // broadphase fully encloses the real circle, whose center sits 48px above the floor.
                Vector2 impactAnchor = Projectile.Center;
                Projectile.width = WingedPlungeHitboxDiameter;
                Projectile.height = WingedPlungeBroadphaseHeight;
                Projectile.Center = impactAnchor;
            }
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage()
            => WingedPlungeExplosion && Age <= WingedPlungeDamageTicks;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!WingedPlungeExplosion)
                return false;

            Vector2 nearestPoint = new Vector2(
                MathHelper.Clamp(ExplosionCenter.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(ExplosionCenter.Y, targetHitbox.Top, targetHitbox.Bottom));
            float radius = WingedPlungeHitboxDiameter * 0.5f;
            return Vector2.DistanceSquared(ExplosionCenter, nearestPoint) <= radius * radius;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Telegraphing)
            {
                if (!TryGetParent(out Projectile parent))
                {
                    Projectile.Kill();
                    return;
                }

                float gather = MathHelper.Clamp(parent.localAI[0] / 55f, 0f, 1f);
                Lighting.AddLight(Projectile.Center - Vector2.UnitY * 12f,
                    0.35f + gather * 0.35f, 0.22f + gather * 0.25f, 0.06f);

                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
                {
                    Vector2 position = Projectile.Center + new Vector2(Main.rand.NextFloat(-60f, 60f), Main.rand.NextFloat(-16f, 4f));
                    Dust mote = Dust.NewDustPerfect(position, Main.rand.NextBool() ? DustID.GoldFlame : DustID.Torch,
                        new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(-3.2f, -1.4f)), 80,
                        new Color(255, 178, 55), Main.rand.NextFloat(0.8f, 1.25f));
                    mote.noGravity = true;
                }
                return;
            }

            float columnFade = ColumnFade(Age);
            Vector2 impactAnchor = ImpactAnchor;
            if (ExplosionOnly)
            {
                Lighting.AddLight(ExplosionCenter,
                    1.35f * columnFade, 0.68f * columnFade, 0.16f * columnFade);
            }
            else
            {
                for (int segment = 0; segment < 5; segment++)
                {
                    Vector2 lightPosition = Projectile.Center - Vector2.UnitY * (ColumnHeight * (segment + 0.5f) / 5f);
                    Lighting.AddLight(lightPosition, 1.15f * columnFade, 0.62f * columnFade, 0.16f * columnFade);
                }
            }

            if (Main.netMode != NetmodeID.Server && Age < 46f)
            {
                for (int i = 0; i < 3; i++)
                {
                    float horizontalRadius = ExplosionOnly ? 76f : 105f;
                    Vector2 position = impactAnchor + new Vector2(
                        Main.rand.NextFloat(-horizontalRadius, horizontalRadius),
                        Main.rand.NextFloat(ExplosionOnly ? -92f : -35f, 6f));
                    Vector2 velocity = ExplosionOnly
                        ? (position - ExplosionCenter).SafeNormalize(-Vector2.UnitY)
                            * Main.rand.NextFloat(2.5f, 7f)
                        : new Vector2(Main.rand.NextFloat(-2.4f, 2.4f), Main.rand.NextFloat(-8f, -3f));
                    Dust ember = Dust.NewDustPerfect(position, Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame,
                        velocity, 40, default, Main.rand.NextFloat(1.1f, 2.15f));
                    ember.noGravity = true;
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WingedPlungeExplosion)
                target.AddBuff(BuffID.OnFire, 5 * 60);
        }

        bool TryGetParent(out Projectile parent)
        {
            parent = null;
            if (ParentIndex < 0 || ParentIndex >= Main.maxProjectiles)
                return false;

            Projectile candidate = Main.projectile[ParentIndex];
            if (!candidate.active || candidate.type != ModContent.ProjectileType<GwynDescentMeteor>())
                return false;

            parent = candidate;
            return true;
        }

        static void LoadAssets()
        {
            solarColumnEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynSolarColumn", AssetRequestMode.ImmediateLoad);
            solarFlowNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_CloudNoise_Tiled", AssetRequestMode.ImmediateLoad);
            flameCurtain ??= ModContent.Request<Texture2D>(TextureRoot + "T_FirePanningCyl45", AssetRequestMode.ImmediateLoad);
            fireFlipbook ??= ModContent.Request<Texture2D>(TextureRoot + "T_fire_flipbook4_sm", AssetRequestMode.ImmediateLoad);
            smokeFlipbook ??= ModContent.Request<Texture2D>(TextureRoot + "T_smoke41_flipbook", AssetRequestMode.ImmediateLoad);
            impactFlare ??= ModContent.Request<Texture2D>(TextureRoot + "T_flare8_vfx", AssetRequestMode.ImmediateLoad);
            groundCracks ??= ModContent.Request<Texture2D>(TextureRoot + "T_Cracks336", AssetRequestMode.ImmediateLoad);
            windStreak ??= ModContent.Request<Texture2D>(TextureRoot + "T_Windstreak3", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            if (Telegraphing)
                DrawTelegraph();
            else
                DrawImpact(ExplosionOnly);

            return false;
        }

        void DrawTelegraph()
        {
            if (!TryGetParent(out Projectile parent))
                return;

            float progress = MathHelper.Clamp(parent.localAI[0] / 55f, 0f, 1f);
            float pulse = 0.78f + 0.22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f);
            Vector2 drawPosition = ImpactAnchor - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            DrawGroundMark(drawPosition, (0.28f + progress * 0.34f) * pulse, 0.62f + progress * 0.18f);
            DrawSolarColumn(drawPosition, 22f + 34f * progress,
                new Color(255, 79, 12), new Color(255, 180, 46), new Color(255, 244, 186),
                (0.16f + progress * 0.24f) * pulse, 0.34f, 0.36f);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        void DrawImpact(bool explosionOnly)
        {
            float columnFade = ColumnFade(Age);
            float smokeFade = SmokeFade(Age);
            Vector2 drawPosition = ImpactAnchor - Main.screenPosition;

            // The smoke flipbook is drawn under the emissive layers so it gives the blast volume
            // without turning the whole column into one opaque sprite.
            DrawSmoke(drawPosition, smokeFade);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (!explosionOnly)
                DrawGroundMark(drawPosition, columnFade, 1.05f + Age * 0.008f);
            DrawImpactRays(drawPosition, columnFade);
            if (!explosionOnly)
                DrawFlameCurtain(drawPosition, columnFade);
            DrawFireball(drawPosition, columnFade);

            // One purpose-built pass now owns the turbulent silhouette, orange/gold body, and
            // white-hot core. Keeping those layers in one shader prevents three generic beams from
            // sliding apart and reading as overlapping laser rectangles.
            if (!explosionOnly)
            {
                DrawSolarColumn(drawPosition, 255f,
                    new Color(255, 44, 5), new Color(255, 139, 18), new Color(255, 239, 174),
                    columnFade, 0.62f, 1.45f);
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        void DrawSmoke(Vector2 drawPosition, float opacity)
        {
            if (opacity <= 0f)
                return;

            Texture2D texture = smokeFlipbook.Value;
            const int columns = 8;
            const int rows = 8;
            int frameWidth = texture.Width / columns;
            int frameHeight = texture.Height / rows;

            for (int i = 0; i < 4; i++)
            {
                int frame = Math.Min(63, (int)(Age * 0.82f) + i * 5);
                Rectangle source = new Rectangle(frame % columns * frameWidth, frame / columns * frameHeight, frameWidth, frameHeight);
                float side = i - 1.5f;
                Vector2 position = drawPosition + new Vector2(side * 45f, -42f - Age * (1.0f + i * 0.08f));
                float scale = 0.62f + i * 0.08f + Age * 0.006f;
                Color color = new Color(98, 58, 46, 205) * (opacity * 0.78f);
                Main.EntitySpriteDraw(texture, position, source, color, side * 0.055f,
                    new Vector2(frameWidth, frameHeight) * 0.5f, scale,
                    i % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            }
        }

        void DrawGroundMark(Vector2 drawPosition, float opacity, float scale)
        {
            Texture2D cracks = groundCracks.Value;
            Texture2D flare = impactFlare.Value;
            Color crackColor = new Color(255, 91, 12) * (opacity * 0.72f);
            Color flareColor = new Color(255, 208, 92) * Math.Min(1f, opacity);

            Main.EntitySpriteDraw(cracks, drawPosition + new Vector2(0f, 3f), null, crackColor,
                0f, cracks.Size() * 0.5f, new Vector2(0.62f, 0.16f) * scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(flare, drawPosition - new Vector2(0f, 12f), null, flareColor,
                MathHelper.PiOver4, flare.Size() * 0.5f, new Vector2(0.68f, 0.25f) * scale, SpriteEffects.None, 0);
        }

        void DrawImpactRays(Vector2 drawPosition, float opacity)
        {
            Texture2D texture = windStreak.Value;
            Vector2 origin = new Vector2(texture.Width * 0.5f, texture.Height);
            for (int i = 0; i < 9; i++)
            {
                float spread = i / 8f;
                float rotation = MathHelper.Lerp(-1.25f, 1.25f, spread);
                float length = 0.58f + 0.28f * (float)Math.Sin((i + 1) * 2.17f);
                Main.EntitySpriteDraw(texture, drawPosition - new Vector2(0f, 12f), null,
                    new Color(255, 154, 34) * (opacity * 0.42f), rotation, origin,
                    new Vector2(0.32f, length), i % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            }
        }

        void DrawFlameCurtain(Vector2 drawPosition, float opacity)
        {
            Texture2D texture = flameCurtain.Value;
            Vector2 origin = new Vector2(texture.Width * 0.5f, texture.Height);
            float flicker = 1f + 0.025f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f);
            Vector2 leftScale = new Vector2(0.30f * flicker, 0.72f);
            Vector2 rightScale = new Vector2(0.25f / flicker, 0.66f);
            int scroll = (int)(Main.GlobalTimeWrappedHourly * 165f) % texture.Height;
            Rectangle risingSource = new Rectangle(0, scroll, texture.Width, texture.Height);
            Rectangle counterSource = new Rectangle(0, texture.Height - scroll, texture.Width, texture.Height);

            Main.EntitySpriteDraw(texture, drawPosition + new Vector2(-28f, 6f), risingSource,
                new Color(255, 54, 5) * (opacity * 0.34f), 0.018f, origin, leftScale,
                SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, drawPosition + new Vector2(32f, 6f), counterSource,
                new Color(255, 151, 18) * (opacity * 0.28f), -0.024f, origin, rightScale,
                SpriteEffects.FlipHorizontally, 0);
        }

        void DrawSolarColumn(
            Vector2 drawPosition,
            float width,
            Color outerColor,
            Color middleColor,
            Color coreColor,
            float opacity,
            float edgeTurbulence,
            float coreStrength)
        {
            if (opacity <= 0f)
                return;

            Effect effect = solarColumnEffect.Value;
            Rectangle source = new Rectangle(0, 0, ColumnHeight, Math.Max(2, (int)width));
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = solarFlowNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.Parameters["OuterColor"].SetValue(outerColor.ToVector3());
                effect.Parameters["MiddleColor"].SetValue(middleColor.ToVector3());
                effect.Parameters["CoreColor"].SetValue(coreColor.ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Size());
                effect.Parameters["EdgeTurbulence"].SetValue(edgeTurbulence);
                effect.Parameters["CoreStrength"].SetValue(coreStrength);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, drawPosition, source, Color.White,
                    -MathHelper.PiOver2, new Vector2(0f, source.Height * 0.5f), 1f, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }
        }

        void DrawFireball(Vector2 drawPosition, float opacity)
        {
            if (Age > 50f || opacity <= 0f)
                return;

            Texture2D texture = fireFlipbook.Value;
            const int columns = 5;
            const int rows = 5;
            int frameWidth = texture.Width / columns;
            int frameHeight = texture.Height / rows;
            int frame = Math.Min(columns * rows - 1, (int)(Age * 0.52f));
            Rectangle source = new Rectangle(frame % columns * frameWidth, frame / columns * frameHeight, frameWidth, frameHeight);
            // Winged Plunge uses this exact Descent explosion frame without its solar column. Its
            // luminous body is 300px across around a 240px circular hitbox: 25% visual overshoot.
            float scale = WingedPlungeExplosion
                ? WingedPlungeVisualDiameter / frameWidth
                : 1.0f + Age * 0.018f;
            Main.EntitySpriteDraw(texture, drawPosition - new Vector2(0f, 48f), source,
                Color.White * Math.Min(1f, opacity * 1.15f), 0f,
                new Vector2(frameWidth, frameHeight) * 0.5f, scale, SpriteEffects.None, 0);
        }

        static float ColumnFade(float age)
        {
            float fadeIn = MathHelper.Clamp(age / 5f, 0f, 1f);
            float fadeOut = MathHelper.Clamp((54f - age) / 18f, 0f, 1f);
            return fadeIn * fadeOut;
        }

        static float SmokeFade(float age)
        {
            float fadeIn = MathHelper.Clamp(age / 9f, 0f, 1f);
            float fadeOut = MathHelper.Clamp((68f - age) / 24f, 0f, 1f);
            return fadeIn * fadeOut;
        }
    }
}

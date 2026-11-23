using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// The Elemental Necromancer's eruption: a 480px (30-tile) column of purple light with a white core and a blue edge,
    /// spawned bottom-anchored on a marked floor patch after its 90-tick dust telegraph. 8t rise, 12t full, 12t fade
    /// (harmless). Each hit adds 35 Miasma buildup. The hitbox is the shader's revealed core (~40px wide), rising from the floor as the shader reveals it.
    /// Spawn its Center at (markX, groundY - Height / 2).
    /// </summary>
    public class ElementalEruptionColumn : ModProjectile
    {
        public const int MiasmaPerHit = 35;
        internal const int Height = 480;
        const int QuadWidth = 72;
        const int RiseTicks = 8;
        const int FullTicks = 12;
        const int FadeTicks = 12;
        const int Lifetime = RiseTicks + FullTicks + FadeTicks;
        // ElementalEruption.fx: a row is revealed once Progress >= (height fraction from the bottom) * 0.72.
        const float EruptionRiseProgress = 0.72f;
        // Shader exactCore is at least 2/3 lit inside |uv.x - 0.5| < 0.28 of the 72px quad, i.e. the middle ~40px.
        const float CoreHalfWidth = 20f;

        static Asset<Effect> eruptionEffect;
        static Asset<Texture2D> smokeTexture;
        static Asset<Texture2D> noiseTexture;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = QuadWidth;
            Projectile.height = Height;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
        }

        int Elapsed => Lifetime - Projectile.timeLeft;

        // 0 -> 1 over the rise; the shader's column is fully revealed once this passes 0.72 + 1/6.
        float RiseProgress => MathHelper.Clamp(Elapsed / (float)RiseTicks, 0f, 1f);

        // At most one hit per player per column: the 20 damaging ticks are shorter than the player's 40-tick post-hit
        // immunity, so a player can only be struck once by this column.
        public override bool? CanDamage() => Projectile.timeLeft > FadeTicks;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float riseFraction = MathHelper.Clamp(RiseProgress / EruptionRiseProgress, 0f, 1f);
            float risenHeight = Height * riseFraction;
            Rectangle core = new Rectangle((int)(Projectile.Center.X - CoreHalfWidth), (int)(Projectile.Bottom.Y - risenHeight),
                (int)(CoreHalfWidth * 2f), (int)risenHeight);
            return core.Intersects(targetHitbox);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Runs on the hit player's machine; MiasmaBuildup.Apply reports the gain to the server, which owns the meter.
            MiasmaBuildup.Apply(target, MiasmaPerHit);
        }

        public override void AI()
        {
            if (Main.dedServ)
            {
                return;
            }

            if (Elapsed == 1)
            {
                // The rumble and 4x layered floor burst as the column breaks the surface (72 motes: 40 body + 18 fast sparks + 14 falling embers).
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f, Pitch = -0.3f }, Projectile.Bottom);

                for (int i = 0; i < 40; i++)
                {
                    int dustType = DustID.PurpleTorch;
                    if (i % 4 == 0)
                    {
                        dustType = DustID.BlueTorch;
                    }
                    else if (i % 6 == 1)
                    {
                        dustType = DustID.Shadowflame;
                    }

                    Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-34f, 34f), Main.rand.NextFloat(-10f, 0f));
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-3.8f, 3.8f), Main.rand.NextFloat(-8.5f, -1.5f));
                    Dust puff = Dust.NewDustPerfect(position, dustType, velocity, 60, default, Main.rand.NextFloat(0.95f, 1.55f));
                    puff.noGravity = true;
                }

                for (int i = 0; i < 18; i++)
                {
                    int dustType = DustID.PurpleTorch;
                    if (i % 3 == 0)
                    {
                        dustType = DustID.AncientLight;
                    }

                    Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-24f, 24f), -4f);
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-4.5f, 4.5f), Main.rand.NextFloat(-14f, -5.5f));
                    Dust glint = Dust.NewDustPerfect(position, dustType, velocity, 40, default, Main.rand.NextFloat(0.55f, 0.95f));
                    glint.noGravity = true;
                }

                // Gravity-affected shards launched up out of the origin so they arc and rain back down.
                for (int i = 0; i < 14; i++)
                {
                    int dustType = DustID.PurpleCrystalShard;
                    if (i % 3 == 0)
                    {
                        dustType = DustID.BlueCrystalShard;
                    }

                    Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-28f, 28f), -4f);
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-3.2f, 3.2f), Main.rand.NextFloat(-9.5f, -5f));
                    Dust ember = Dust.NewDustPerfect(position, dustType, velocity, 60, default, Main.rand.NextFloat(0.95f, 1.35f));
                    ember.noGravity = false;
                }
            }

            // Violet light along the risen part of the column.
            float riseFraction = MathHelper.Clamp(RiseProgress / EruptionRiseProgress, 0f, 1f);
            float risenHeight = Height * riseFraction;
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 lightPoint = Projectile.Bottom - new Vector2(0f, risenHeight * (i + 0.5f) / 4f);
                Lighting.AddLight(lightPoint, 0.55f * fade, 0.3f * fade, 0.95f * fade);
            }

            // Ongoing origin boil + upward streaks overlapping the shader + gravity embers that shoot up and rain back down.
            if (Projectile.timeLeft > FadeTicks)
            {
                for (int i = 0; i < 3; i++)
                {
                    int dustType = DustID.PurpleTorch;
                    if (i == 2)
                    {
                        dustType = DustID.BlueTorch;
                    }

                    Vector2 basePos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-28f, 28f), Main.rand.NextFloat(-6f, 0f));
                    Vector2 baseVel = new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-5.5f, -1.5f));
                    Dust boil = Dust.NewDustPerfect(basePos, dustType, baseVel, 60, default, Main.rand.NextFloat(0.9f, 1.35f));
                    boil.noGravity = true;
                }

                for (int i = 0; i < 4; i++)
                {
                    int dustType = DustID.PurpleTorch;
                    if (i == 1)
                    {
                        dustType = DustID.BlueTorch;
                    }
                    else if (i == 3)
                    {
                        dustType = DustID.AncientLight;
                    }

                    float heightOffset = Main.rand.NextFloat(0f, risenHeight * 0.65f);
                    Vector2 streakPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-CoreHalfWidth * 1.15f, CoreHalfWidth * 1.15f), -heightOffset);
                    Vector2 streakVel = new Vector2(Main.rand.NextFloat(-1.1f, 1.1f), Main.rand.NextFloat(-19f, -9.5f));
                    float scale = Main.rand.NextFloat(0.85f, 1.4f);
                    if (dustType == DustID.AncientLight)
                    {
                        scale = Main.rand.NextFloat(0.55f, 0.9f);
                    }

                    Dust spark = Dust.NewDustPerfect(streakPos, dustType, streakVel, 40, default, scale);
                    spark.noGravity = true;
                }

                // Gravity embers shot upward along the column: rise ~30-60t, crest, then fall back down after the column fades.
                for (int i = 0; i < 3; i++)
                {
                    int dustType = DustID.Shadowflame;
                    if (i == 1)
                    {
                        dustType = DustID.PurpleCrystalShard;
                    }
                    else if (i == 2)
                    {
                        dustType = DustID.BlueCrystalShard;
                    }

                    float heightOffset = Main.rand.NextFloat(0f, risenHeight * 0.85f);
                    Vector2 rainPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-CoreHalfWidth * 1.25f, CoreHalfWidth * 1.25f), -heightOffset);
                    Vector2 rainVel = new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-7.5f, -3.5f));
                    Dust falling = Dust.NewDustPerfect(rainPos, dustType, rainVel, 50, default, Main.rand.NextFloat(0.95f, 1.35f));
                    falling.noGravity = false;
                }
            }
            else if (Projectile.timeLeft > FadeTicks / 2)
            {
                // Tail shower during the first half of fadeout so falling embers linger naturally as the shader dissolves.
                for (int i = 0; i < 2; i++)
                {
                    int dustType = DustID.PurpleCrystalShard;
                    if (i == 1)
                    {
                        dustType = DustID.BlueCrystalShard;
                    }

                    Vector2 tailPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-CoreHalfWidth, CoreHalfWidth), -Main.rand.NextFloat(Height * 0.2f, Height * 0.95f));
                    Vector2 tailVel = new Vector2(Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextFloat(-3.5f, -1f));
                    Dust linger = Dust.NewDustPerfect(tailPos, dustType, tailVel, 70, default, Main.rand.NextFloat(0.85f, 1.2f));
                    linger.noGravity = false;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            eruptionEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/ElementalEruption", AssetRequestMode.ImmediateLoad);
            smokeTexture ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_smoke_b7", AssetRequestMode.ImmediateLoad);
            noiseTexture ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_VFX_Noise41", AssetRequestMode.ImmediateLoad);

            // Same draw contract as ArtoriasVFX.DrawEruption: the primary texture IS the quad, its source rect is the
            // draw size (clamped to the texture), additive blend, 4px gameplay-pixel blocks.
            Texture2D smoke = smokeTexture.Value;
            Vector2 drawSize = Projectile.Size;
            int sourceWidth = System.Math.Clamp((int)drawSize.X, 1, smoke.Width);
            int sourceHeight = System.Math.Clamp((int)drawSize.Y, 1, smoke.Height);
            Rectangle source = new Rectangle(0, 0, sourceWidth, sourceHeight);
            Vector2 sourceSize = source.Size();
            Vector2 scale = drawSize / sourceSize;
            const float PixelBlockSize = 4f;
            Vector2 pixelBlocks = Vector2.Max(drawSize / PixelBlockSize, Vector2.One);
            // 12-tick fadeout: scale both RGB (via drawColor) and Opacity by fade so the overbright white core (intensity ~2.0)
            // dissolves cleanly across the full 12 ticks instead of staying clipped to white until the last 2-3 frames.
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            float opacity = 0.92f * fade;
            Color drawTint = new Color(fade, fade, fade, 1f);

            Effect effect = eruptionEffect.Value;
            effect.CurrentTechnique = effect.Techniques["ElementalEruption"];

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = noiseTexture.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                effect.Parameters["DarkColor"]?.SetValue(new Color(40, 10, 84).ToVector3());
                effect.Parameters["MidColor"]?.SetValue(new Color(150, 60, 225).ToVector3());
                effect.Parameters["CoreColor"]?.SetValue(Color.White.ToVector3());
                effect.Parameters["Opacity"]?.SetValue(opacity);
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"]?.SetValue(RiseProgress);
                effect.Parameters["DrawSize"]?.SetValue(sourceSize);
                effect.Parameters["PrimaryTextureSize"]?.SetValue(smoke.Size());
                effect.Parameters["PixelGrid"]?.SetValue(new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y));
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(smoke, Projectile.Center - Main.screenPosition, source, drawTint,
                    0f, sourceSize * 0.5f, scale, SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 24; i++)
            {
                Vector2 position = Projectile.Bottom - new Vector2(Main.rand.NextFloat(-CoreHalfWidth, CoreHalfWidth), Main.rand.NextFloat(0f, Height));
                Dust mote = Dust.NewDustPerfect(position, DustID.PurpleTorch, Main.rand.NextVector2Circular(4f, 4f), 60, default, 1.3f);
                mote.noGravity = true;
            }
        }
    }
}

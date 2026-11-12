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
    /// spawned bottom-anchored on a marked floor patch after its 90-tick dust telegraph. 8t rise, 12t full, 8t fade
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
        const int FadeTicks = 8;
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
                // The rumble and floor puff as it breaks the surface.
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f, Pitch = -0.3f }, Projectile.Bottom);
                for (int i = 0; i < 18; i++)
                {
                    Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-24f, 24f), -4f);
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-5f, -1f));
                    Dust puff = Dust.NewDustPerfect(position, DustID.PurpleTorch, velocity, 60, default, 1.5f);
                    puff.noGravity = true;
                }
            }

            // Violet light along the risen part of the column.
            float riseFraction = MathHelper.Clamp(RiseProgress / EruptionRiseProgress, 0f, 1f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 lightPoint = Projectile.Bottom - new Vector2(0f, Height * riseFraction * (i + 0.5f) / 4f);
                Lighting.AddLight(lightPoint, 0.55f * fade, 0.3f * fade, 0.95f * fade);
            }

            if (Projectile.timeLeft % 2 == 0)
            {
                Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-CoreHalfWidth, CoreHalfWidth), 0f);
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-16f, -6f));
                Dust spark = Dust.NewDustPerfect(position, DustID.PurpleTorch, velocity, 40, default, 1.4f);
                spark.noGravity = true;
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
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            float opacity = 0.92f * fade;

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

                Main.EntitySpriteDraw(smoke, Projectile.Center - Main.screenPosition, source, Color.White,
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

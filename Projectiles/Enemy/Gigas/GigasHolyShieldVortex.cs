using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>One end of Gigas's Holy Shield route. Intake vortices briefly swallow a friendly shot
    ///on the giant; output vortices telegraph for 60 ticks behind the target, then release that exact
    ///projectile back through the vortex. ai[0] is the stored projectile index for outputs; ai[1]
    ///selects output mode; Projectile.owner is the player being targeted.</summary>
    class GigasHolyShieldVortex : ModProjectile
    {
        public const float OutputMode = 1f;
        const int IntakeTicks = 20;
        const int OutputTelegraphTicks = 60;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> vortexEffect;
        static Asset<Texture2D> macroNoise;
        static Asset<Texture2D> detailNoise;

        bool IsOutput => Projectile.ai[1] == OutputMode;
        int ReflectedProjectileIndex => (int)Projectile.ai[0];
        int TargetPlayer => Projectile.owner;
        int Duration => IsOutput ? OutputTelegraphTicks : IntakeTicks;
        float Progress => MathHelper.Clamp(Projectile.localAI[0] / Duration, 0f, 1f);

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = OutputTelegraphTicks;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Duration;
        }

        static void LoadAssets()
        {
            vortexEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasHaloSun", AssetRequestMode.ImmediateLoad);
            macroNoise ??= ModContent.Request<Texture2D>(TextureRoot + "SmoothNoise", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            int dustCount = IsOutput ? 1 + (int)(Progress * 2f) : 2;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(16f, 34f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GoldFlame,
                    (-offset).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.8f, 2.2f), 110, default,
                    Main.rand.NextFloat(0.48f, 0.88f));
                mote.noGravity = true;
            }
            if (Main.rand.NextBool(3))
            {
                Dust glint = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f),
                    DustID.AncientLight, Main.rand.NextVector2Circular(0.3f, 0.3f), 40, Color.LightGoldenrodYellow,
                    Main.rand.NextFloat(0.42f, 0.72f));
                glint.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.38f + Progress * 0.28f, 0.30f + Progress * 0.22f, 0.08f);

            if (IsOutput && Projectile.localAI[0] >= OutputTelegraphTicks)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && TargetPlayer >= 0 && TargetPlayer < Main.maxPlayers)
                {
                    Player player = Main.player[TargetPlayer];
                    if (player.active && !player.dead && ReflectedProjectileIndex >= 0 && ReflectedProjectileIndex < Main.maxProjectiles)
                    {
                        Projectile reflected = Main.projectile[ReflectedProjectileIndex];
                        if (reflected.active)
                        {
                            float speed = Projectile.localAI[1] > 0f ? Projectile.localAI[1] : 14f;
                            reflected.Center = Projectile.Center;
                            reflected.friendly = false;
                            reflected.hostile = true;
                            reflected.hide = false;
                            reflected.tileCollide = false;
                            reflected.ignoreWater = true;
                            reflected.velocity = (player.Center - reflected.Center).SafeNormalize(Vector2.UnitX) * speed;
                            reflected.netUpdate = true;
                        }
                    }
                }
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.45f, Pitch = 0.35f }, Projectile.Center);
                }
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            float grow = Progress * (2f - Progress);
            float fade = IsOutput ? 0.38f + grow * 0.52f : (1f - Progress) * 0.8f;
            float diameter = IsOutput ? 72f : 52f;
            Texture2D primary = macroNoise.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousTexture = device.Textures[1];
            SamplerState previousSampler = device.SamplerStates[1];
            try
            {
                device.Textures[1] = detailNoise.Value;
                device.SamplerStates[1] = SamplerState.LinearWrap;
                Effect effect = vortexEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GigasHaloSun"];
                effect.Parameters["OuterColor"].SetValue(new Color(105, 62, 5).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(255, 181, 30).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 249, 201).ToVector3());
                effect.Parameters["Opacity"].SetValue(fade);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Phase"].SetValue(Projectile.identity * 0.173f);
                effect.Parameters["Active"].SetValue(0f);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White, 0f,
                    primary.Size() * 0.5f, diameter * grow / primary.Width, SpriteEffects.None, 0);
            }
            finally
            {
                device.Textures[1] = previousTexture;
                device.SamplerStates[1] = previousSampler;
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }
    }
}

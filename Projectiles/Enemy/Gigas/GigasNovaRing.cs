using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas Wrath of Gold nova: an expanding filled solar field. Its 300px radius makes the true
    ///damage diameter 600px; each player may only be hit once. ai[0] = max radius (px), ai[1] =
    ///optional duration; ai[2] marks a visual-only nested layer. Solar Slabs supplies a 24-tick
    ///outer damage nova plus a simultaneous 75%-scale visual core; Wrath keeps its normal expansion.
    ///</summary>
    class GigasNovaRing : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float ExpandSpeed = 9f;
        const float RingHalfThickness = 22f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";
        static Asset<Effect> novaEffect;
        static Asset<Texture2D> macroNoise;
        static Asset<Texture2D> detailNoise;
        static Asset<Texture2D> flameNoise;
        readonly bool[] hitPlayers = new bool[Main.maxPlayers];

        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 300f;
        int Duration => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : (int)(MaxRadius / ExpandSpeed) + 2;
        float Radius => MaxRadius * MathHelper.Clamp(Projectile.localAI[0] / Duration, 0f, 1f);
        // Long scripted novas fade over their last 30 ticks. A 24-tick Solar Slabs burst instead
        // holds at full brightness, then sheds quickly over its last ~8 ticks.
        float FadeTicks => Projectile.ai[1] > 0f ? Math.Min(30f, Math.Max(6f, Duration * 0.35f)) : 5f;
        bool VisualOnly => Projectile.ai[2] > 0.5f;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 600; // broadphase matches the 600px maximum damage diameter
            Projectile.height = 600;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 40;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Duration;
            Projectile.hostile = !VisualOnly;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            //Dust ring at the current radius — denser near the start so the burst reads as a flash
            int points = Duration > 60 ? 20 : 36;
            for (int i = 0; i < points; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * (Radius + Main.rand.NextFloat(-8f, 8f));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 80, default, Main.rand.NextFloat(1.3f, 1.9f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = angle.ToRotationVector2() * 2.5f;
            }
            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * Radius;
                int sparkle = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, -1f, 0, default, 1.1f);
                Main.dust[sparkle].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 1.2f, 1f, 0.4f);
        }

        static void LoadAssets()
        {
            novaEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasNovaRing", AssetRequestMode.ImmediateLoad);
            macroNoise ??= ModContent.Request<Texture2D>(TextureRoot + "SmoothNoise", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
            flameNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_FirePanningCyl45", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            float drawRadius = Radius + RingHalfThickness + 16f;
            int diameter = (int)Math.Ceiling(drawRadius * 2f);
            Texture2D primary = macroNoise.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            Texture previousFlameTexture = graphicsDevice.Textures[2];
            SamplerState previousFlameSampler = graphicsDevice.SamplerStates[2];
            try
            {
                graphicsDevice.Textures[1] = detailNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                graphicsDevice.Textures[2] = flameNoise.Value;
                graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;
                Effect effect = novaEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GigasNovaSun"];
                effect.Parameters["OuterColor"].SetValue((VisualOnly ? new Color(136, 79, 6) : new Color(105, 61, 5)).ToVector3());
                effect.Parameters["MiddleColor"].SetValue((VisualOnly ? new Color(255, 205, 55) : new Color(255, 176, 25)).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 245, 190).ToVector3());
                effect.Parameters["Opacity"].SetValue(MathHelper.Clamp(Projectile.timeLeft / FadeTicks, 0f, 1f));
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(new Vector2(diameter));
                effect.Parameters["RingRadius"].SetValue(Radius);
                effect.Parameters["PixelBlockSize"].SetValue(2f);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White, 0f,
                    primary.Size() * 0.5f, diameter / (float)primary.Width, SpriteEffects.None, 0);

                effect.CurrentTechnique = effect.Techniques["GigasNovaCorona"];
                float coronaDiameter = diameter + 152f;
                effect.Parameters["DrawSize"].SetValue(new Vector2(coronaDiameter));
                effect.Parameters["PixelBlockSize"].SetValue(2f);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White, 0f,
                    primary.Size() * 0.5f, coronaDiameter / primary.Width, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                graphicsDevice.Textures[2] = previousFlameTexture;
                graphicsDevice.SamplerStates[2] = previousFlameSampler;
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (VisualOnly)
            {
                return false;
            }
            //Distance from the field center to the closest point of the target's hitbox.
            Vector2 closest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float distClosest = Vector2.Distance(Projectile.Center, closest);
            return distClosest <= Radius;
        }

        public override bool CanHitPlayer(Player target) => !VisualOnly && !hitPlayers[target.whoAmI];

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            hitPlayers[target.whoAmI] = true;
        }
    }
}

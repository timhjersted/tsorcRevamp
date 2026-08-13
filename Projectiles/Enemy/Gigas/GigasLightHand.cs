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
    ///Gigas grasping light: two tall slabs of golden light rise from the ground at the player's
    ///flanks and converge, clapping together at the point where the player was standing. Damage is
    ///only in the seam during the clap window — walk/roll out sideways before they meet.
    ///ai[0] = telegraph (converge) ticks. One projectile manages both hands as dust sculptures.
    ///</summary>
    class GigasLightHand : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float HandStartOffset = 100f;
        const float HandHeight = 130f;
        const float SlabWidth = 74f;
        const float AuraWidth = 116f;
        const float AuraHeight = 154f;
        const float NovaDamageRadius = 72f;
        const int ClapTicks = 10;
        const string TextureRoot = "tsorcRevamp/Textures/";

        static Asset<Effect> handEffect;
        static Asset<Effect> novaEffect;
        static Asset<Texture2D> monolithTexture;
        static Asset<Texture2D> flowNoise;
        static Asset<Texture2D> crackNoise;
        static Asset<Texture2D> novaDetailNoise;
        static Asset<Texture2D> novaFlameNoise;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 50;
        bool Clapping => Projectile.localAI[0] > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 70;
            Projectile.height = 130;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TelegraphTicks + ClapTicks;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.5f }, Projectile.Center);
        }

        public override bool? CanDamage()
        {
            return Clapping;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Clapping)
            {
                return false;
            }

            // The original vertical clap seam remains dangerous, and the expanding nova hitbox
            // follows the visible solar body rather than granting its decorative corona damage.
            if (projHitbox.Intersects(targetHitbox))
            {
                return true;
            }
            Vector2 closestPoint = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float clapProgress = MathHelper.Clamp((Projectile.localAI[0] - TelegraphTicks) / ClapTicks, 0f, 1f);
            float novaRadius = MathHelper.Lerp(NovaDamageRadius * 0.48f, NovaDamageRadius, clapProgress);
            return Vector2.DistanceSquared(Projectile.Center, closestPoint) <= novaRadius * novaRadius;
        }

        static void LoadAssets()
        {
            handEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasLightHand", AssetRequestMode.ImmediateLoad);
            novaEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasNovaRing", AssetRequestMode.ImmediateLoad);
            monolithTexture ??= ModContent.Request<Texture2D>(TextureRoot + "Particles/GigasConsecratedMonolith", AssetRequestMode.ImmediateLoad);
            flowNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/SmoothNoise", AssetRequestMode.ImmediateLoad);
            crackNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/Vein_02-512x512", AssetRequestMode.ImmediateLoad);
            novaDetailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
            novaFlameNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/T_FirePanningCyl45", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            float progress = MathHelper.Clamp(Projectile.localAI[0] / TelegraphTicks, 0f, 1f);
            float rise = MathHelper.Min(1f, progress * 2f);
            float offset = MathHelper.Lerp(HandStartOffset, 12f, progress * progress);
            float bottom = Projectile.Center.Y + HandHeight / 2f;
            float opacity = Clapping ? 0.94f : 0.48f + progress * 0.32f;
            Texture2D monolith = monolithTexture.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousFlow = device.Textures[1];
            SamplerState previousFlowSampler = device.SamplerStates[1];
            Texture previousCrack = device.Textures[2];
            SamplerState previousCrackSampler = device.SamplerStates[2];
            try
            {
                device.Textures[1] = flowNoise.Value;
                device.SamplerStates[1] = SamplerState.LinearWrap;
                device.Textures[2] = crackNoise.Value;
                device.SamplerStates[2] = SamplerState.LinearWrap;
                Effect effect = handEffect.Value;
                effect.Parameters["GoldColor"].SetValue(new Color(187, 108, 12).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 249, 211).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["PrimaryTextureSize"].SetValue(monolith.Size());
                effect.CurrentTechnique = effect.Techniques["GigasLightHandAura"];
                for (int side = -1; side <= 1; side += 2)
                {
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.EntitySpriteDraw(monolith, new Vector2(Projectile.Center.X + side * offset, bottom) - Main.screenPosition,
                        null, Color.White, 0f, new Vector2(monolith.Width * 0.5f, monolith.Height),
                        new Vector2(AuraWidth / monolith.Width, AuraHeight * rise / monolith.Height),
                        side > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
                }

                if (Clapping)
                {
                    DrawImpactNova(device);
                }
            }
            finally
            {
                device.Textures[1] = previousFlow;
                device.SamplerStates[1] = previousFlowSampler;
                device.Textures[2] = previousCrack;
                device.SamplerStates[2] = previousCrackSampler;
            }

            // The pixel-art mask remains sharp and translucent above the filtered aura and nova.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            for (int side = -1; side <= 1; side += 2)
            {
                Main.EntitySpriteDraw(monolith, new Vector2(Projectile.Center.X + side * offset, bottom) - Main.screenPosition,
                    null, new Color(255, 234, 177, 112), 0f, new Vector2(monolith.Width * 0.5f, monolith.Height),
                    new Vector2(SlabWidth / monolith.Width, HandHeight * rise / monolith.Height),
                    side > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        void DrawImpactNova(GraphicsDevice device)
        {
            float clapProgress = MathHelper.Clamp((Projectile.localAI[0] - TelegraphTicks) / ClapTicks, 0f, 1f);
            float novaRadius = MathHelper.Lerp(NovaDamageRadius * 0.48f, NovaDamageRadius, clapProgress);
            int diameter = (int)Math.Ceiling((novaRadius + 30f) * 2f);
            Effect effect = novaEffect.Value;
            Texture2D primary = flowNoise.Value;
            device.Textures[1] = novaDetailNoise.Value;
            device.SamplerStates[1] = SamplerState.LinearWrap;
            device.Textures[2] = novaFlameNoise.Value;
            device.SamplerStates[2] = SamplerState.LinearWrap;
            float opacity = 0.94f - clapProgress * 0.42f;
            effect.Parameters["OuterColor"].SetValue(new Color(105, 61, 5).ToVector3());
            effect.Parameters["MiddleColor"].SetValue(new Color(255, 176, 25).ToVector3());
            effect.Parameters["CoreColor"].SetValue(new Color(255, 245, 190).ToVector3());
            effect.Parameters["Opacity"].SetValue(opacity);
            effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["DrawSize"].SetValue(new Vector2(diameter));
            effect.Parameters["RingRadius"].SetValue(novaRadius);
            effect.CurrentTechnique = effect.Techniques["GigasNovaSun"];
            effect.CurrentTechnique.Passes[0].Apply();
            Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White, 0f,
                primary.Size() * 0.5f, diameter / (float)primary.Width, SpriteEffects.None, 0);

            int coronaDiameter = diameter + 48;
            effect.Parameters["DrawSize"].SetValue(new Vector2(coronaDiameter));
            effect.CurrentTechnique = effect.Techniques["GigasNovaCorona"];
            effect.CurrentTechnique.Passes[0].Apply();
            Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White, 0f,
                primary.Size() * 0.5f, coronaDiameter / (float)primary.Width, SpriteEffects.None, 0);
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (!Clapping)
            {
                //Two converging hand-slabs of light, rising from the ground line
                float progress = Projectile.localAI[0] / (float)TelegraphTicks;
                float offset = MathHelper.Lerp(HandStartOffset, 12f, progress * progress); //accelerating convergence
                float bottom = Projectile.Center.Y + HandHeight / 2f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = Projectile.Center.X + side * offset;
                    for (int i = 0; i < 3; i++)
                    {
                        //Rise the slab in with progress: dust only up to the current height
                        float y = bottom - Main.rand.NextFloat(HandHeight * MathHelper.Min(1f, progress * 2f));
                        int dust = Dust.NewDust(new Vector2(x - 5f, y), 10, 4, DustID.GoldFlame, 0f, 0f, 90, default, 1.3f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity = new Vector2(-side * 0.4f, -0.8f);
                    }
                    //"Fingers": brighter sparkles crowning the slab
                    if (Main.rand.NextBool(2))
                    {
                        float y = bottom - HandHeight * MathHelper.Min(1f, progress * 2f);
                        int sparkle = Dust.NewDust(new Vector2(x - 6f, y), 12, 6, DustID.GoldCoin, 0f, -1f, 0, default, 1f);
                        Main.dust[sparkle].noGravity = true;
                    }
                }
                Lighting.AddLight(Projectile.Center, 0.5f * progress, 0.45f * progress, 0.2f * progress);

                if (Projectile.localAI[0] >= TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = 0.3f }, Projectile.Center);
                    UsefulFunctions.ScreenShake(Projectile.Center, 5f, 12);
                }
                return;
            }

            //Clap: a single blazing seam where the hands met
            if (Projectile.localAI[0] == TelegraphTicks + 1)
            {
                SpawnImpactBurst();
            }
            for (int i = 0; i < 10; i++)
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-14f, 14f), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 50, default, Main.rand.NextFloat(1.6f, 2.2f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-5f, -2f));
            }
            Lighting.AddLight(Projectile.Center, 1f, 0.9f, 0.4f);
        }

        void SpawnImpactBurst()
        {
            // 42 coarse flames (0.75-1.30), 18 fine coins (0.65-1.00), and 10 bright motes
            // (0.55-0.85): all stay below the 2px-scale chunkiness ceiling while forming three
            // distinct outward-moving layers instead of one oversized dust stamp.
            for (int i = 0; i < 42; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.0f, 5.0f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 22f), DustID.GoldFlame,
                    velocity, 50, default, Main.rand.NextFloat(0.75f, 1.30f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.8f, 6.2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 18f), DustID.GoldCoin,
                    velocity, 0, default, Main.rand.NextFloat(0.65f, 1.00f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 4.3f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 14f), DustID.AncientLight,
                    velocity, 20, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.55f, 0.85f));
                dust.noGravity = true;
            }
        }
    }
}

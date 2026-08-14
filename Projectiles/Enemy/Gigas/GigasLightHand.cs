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
    ///Gigas Solar Slabs: two tall solar monoliths rise far from the player's flanks, hold their
    ///position for a readable pause, then converge in a nova clap. The inner seam and compact nova
    ///body are hostile; the larger flame envelope and nova corona are decorative. ai[0] = movement ticks after the hold.
    ///</summary>
    class GigasLightHand : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        // The pair begins 800px apart: each slab is 400px from the committed strike point.
        const float HandStartOffset = 400f;
        const float HandHeight = 182f;
        const float SlabWidth = 104f;
        const float AuraWidth = 221f;
        const float AuraHeight = 305f;
        const float NovaDamageRadius = 300f;
        const int PreAppearanceTelegraphTicks = 60;
        const int HoldTicks = 60;
        const int RiseTicks = 18;
        // The double nova is a sharp release, not a long lingering field: it reaches full size
        // and clears in under half a second. The inner 75% layer provides density without damage.
        const int ClapTicks = 24;
        const string TextureRoot = "tsorcRevamp/Textures/";

        static Asset<Effect> handEffect;
        static Asset<Texture2D> monolithTexture;
        static Asset<Texture2D> flowNoise;
        static Asset<Texture2D> crackNoise;

        int MovementTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 50;
        int TotalTelegraphTicks => PreAppearanceTelegraphTicks + HoldTicks + MovementTicks;
        bool Clapping => Projectile.localAI[0] > TotalTelegraphTicks;
        bool SlabsConverging => !Clapping && Projectile.localAI[0] > PreAppearanceTelegraphTicks + HoldTicks;
        float MovementProgress => MathHelper.Clamp((Projectile.localAI[0] - PreAppearanceTelegraphTicks - HoldTicks) / MovementTicks, 0f, 1f);
        float AppearanceProgress => MathHelper.Clamp((Projectile.localAI[0] - PreAppearanceTelegraphTicks) / RiseTicks, 0f, 1f);
        float SlabBottom => Projectile.Center.Y + 24f;
        float CurrentSlabOffset => MathHelper.Lerp(HandStartOffset, 12f, MovementProgress * MovementProgress);

        Rectangle SlabBody(int side) => new Rectangle(
            (int)(Projectile.Center.X + side * CurrentSlabOffset - SlabWidth * 0.5f),
            (int)(SlabBottom - HandHeight), (int)SlabWidth, (int)HandHeight);

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            // Broadphase covers the two real slab bodies at their furthest separation. The separate
            // 600px GigasNovaRing owns the clap's circular damage and draw pass.
            Projectile.width = (int)(HandStartOffset * 2f + SlabWidth + 16f);
            Projectile.height = (int)Math.Max(NovaDamageRadius * 2f, HandHeight);
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TotalTelegraphTicks + ClapTicks;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.5f }, Projectile.Center);
        }

        public override bool? CanDamage()
        {
            return SlabsConverging || Clapping;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // The actual monolith sprites, rather than their yellow flame envelopes, own the
            // moving contact damage. They remain harmful once the bodies overlap at the clap.
            for (int side = -1; side <= 1; side += 2)
            {
                if (SlabBody(side).Intersects(targetHitbox))
                {
                    return true;
                }
            }

            return false;
        }

        static void LoadAssets()
        {
            handEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasLightHand", AssetRequestMode.ImmediateLoad);
            monolithTexture ??= ModContent.Request<Texture2D>(TextureRoot + "Particles/GigasConsecratedMonolith", AssetRequestMode.ImmediateLoad);
            flowNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/SmoothNoise", AssetRequestMode.ImmediateLoad);
            crackNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/Vein_02-512x512", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            float progress = MovementProgress;
            float rise = AppearanceProgress;
            if (rise <= 0f)
            {
                return false;
            }
            float offset = MathHelper.Lerp(HandStartOffset, 12f, progress * progress);
            float bottom = SlabBottom;
            float clapFade = Clapping
                ? 1f - MathHelper.Clamp((Projectile.localAI[0] - TotalTelegraphTicks) / ClapTicks, 0f, 1f)
                : 1f;
            float opacity = (Clapping ? 0.94f : 0.56f + rise * 0.24f) * clapFade;
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
                effect.Parameters["DrawSize"].SetValue(new Vector2(AuraWidth, AuraHeight * rise));
                effect.Parameters["PixelBlockSize"].SetValue(2f);
                effect.CurrentTechnique = effect.Techniques["GigasLightHandAura"];
                for (int side = -1; side <= 1; side += 2)
                {
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.EntitySpriteDraw(monolith, new Vector2(Projectile.Center.X + side * offset, bottom) - Main.screenPosition,
                        null, Color.White, 0f, new Vector2(monolith.Width * 0.5f, monolith.Height),
                        new Vector2(AuraWidth / monolith.Width, AuraHeight * rise / monolith.Height),
                        side > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
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
                    null, new Color(255, 234, 177, (int)(112f * clapFade)), 0f, new Vector2(monolith.Width * 0.5f, monolith.Height),
                    new Vector2(SlabWidth / monolith.Width, HandHeight * rise / monolith.Height),
                    side > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (!Clapping)
            {
                // For 60 ticks, gold motes gather at the future slab positions while the slabs stay
                // completely invisible. They then fade/rise in over 18 ticks, hold for 60, and converge.
                float progress = MovementProgress;
                float rise = AppearanceProgress;
                float offset = MathHelper.Lerp(HandStartOffset, 12f, progress * progress); //accelerating convergence
                float bottom = SlabBottom;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = Projectile.Center.X + side * offset;
                    int dustCount = rise > 0f ? 3 : 1;
                    for (int i = 0; i < dustCount; i++)
                    {
                        float y = bottom - Main.rand.NextFloat(HandHeight * Math.Max(rise, 0.18f));
                        int dust = Dust.NewDust(new Vector2(x - 5f, y), 10, 4, DustID.GoldFlame, 0f, 0f, 90, default, 1.3f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity = rise > 0f
                            ? new Vector2(-side * 0.4f, -0.8f)
                            : new Vector2(-side * 0.18f, -1.25f);
                    }
                    //"Fingers": brighter sparkles crowning the slab
                    if (rise > 0f && Main.rand.NextBool(2))
                    {
                        float y = bottom - HandHeight * rise;
                        int sparkle = Dust.NewDust(new Vector2(x - 6f, y), 12, 6, DustID.GoldCoin, 0f, -1f, 0, default, 1f);
                        Main.dust[sparkle].noGravity = true;
                    }
                }
                Lighting.AddLight(Projectile.Center, 0.5f * rise, 0.45f * rise, 0.2f * rise);

                if (Projectile.localAI[0] >= TotalTelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = 0.3f }, Projectile.Center);
                    UsefulFunctions.ScreenShake(Projectile.Center, 5f, 12);
                }
                return;
            }

            // The impact is two simultaneous pixel-filtered solar novas: the 600px outer field owns
            // damage, while the 450px inner field is visual-only and makes the brief burst feel dense.
            if (Projectile.localAI[0] == TotalTelegraphTicks + 1)
            {
                SpawnImpactBurst();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<GigasNovaRing>(), Projectile.damage, Projectile.knockBack,
                        Main.myPlayer, NovaDamageRadius, ClapTicks);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<GigasNovaRing>(), 0, 0f,
                        Main.myPlayer, NovaDamageRadius * 0.75f, ClapTicks, 1f);
                }
            }
            for (int i = 0; i < 10; i++)
            {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(18f, HandHeight * 0.5f);
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

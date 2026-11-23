using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Gigas
{
    ///<summary>
    ///Gigas Solar Slabs: two tall solar monoliths rise far from the player's flanks, hold their
    ///position for a readable pause, then converge and detonate in a layered golden solar burst.
    ///The monolith sprite matches the moving hitbox (just ~2 tiles inside the flame aura), and the
    ///clap detonation uses Effects/GigasSolarBurst.fx rather than Wrath of Gold's slower fire shader.
    ///ai[0] = movement ticks after the hold.
    ///</summary>
    class GigasLightHand : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        // The pair begins 800px apart: each slab is 400px from the committed strike point.
        // Sprite and hitbox are ~2 tiles smaller than the 221x305 aura quad so the flame rim hugs
        // the stone body tightly while keeping the damaging slab visually honest.
        const float HandStartOffset = 400f;
        const float AuraWidth = 221f;
        const float AuraHeight = 305f;
        const float SlabWidth = 180f;
        const float HandHeight = 273f;
        const float NovaDamageRadius = 300f;
        const int PreAppearanceTelegraphTicks = 60;
        const int HoldTicks = 60;
        const int RiseTicks = 18;
        const int SlabClapFadeTicks = 20;
        const int BurstDamageTicks = 24;
        const int BurstVisualTicks = 46;
        const string TextureRoot = "tsorcRevamp/Textures/";

        static Asset<Effect> handEffect;
        static Asset<Effect> burstEffect;
        static Asset<Texture2D> monolithTexture;
        static Asset<Texture2D> flowNoise;
        static Asset<Texture2D> crackNoise;
        static Asset<Texture2D> billowNoise;
        static Asset<Texture2D> shockNoise;
        static Asset<Texture2D> sparkNoise;
        static Asset<Texture2D> flare616Centered;
        static Asset<Texture2D> flare666Centered;

        readonly bool[] burstHitPlayers = new bool[Main.maxPlayers];

        int MovementTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 50;
        int TotalTelegraphTicks => PreAppearanceTelegraphTicks + HoldTicks + MovementTicks;
        bool Clapping => Projectile.localAI[0] > TotalTelegraphTicks;
        float ClapAge => Math.Max(0f, Projectile.localAI[0] - TotalTelegraphTicks);
        bool SlabsConverging => !Clapping && Projectile.localAI[0] > PreAppearanceTelegraphTicks + HoldTicks;
        float MovementProgress => MathHelper.Clamp((Projectile.localAI[0] - PreAppearanceTelegraphTicks - HoldTicks) / MovementTicks, 0f, 1f);
        float AppearanceProgress => MathHelper.Clamp((Projectile.localAI[0] - PreAppearanceTelegraphTicks) / RiseTicks, 0f, 1f);
        float SlabBottom => Projectile.Center.Y + 24f;
        float CurrentSlabOffset => MathHelper.Lerp(HandStartOffset, 12f, MovementProgress * MovementProgress);

        float CurrentBurstRadius()
        {
            float progress = MathHelper.Clamp(ClapAge / BurstDamageTicks, 0f, 1f);
            float pop = progress * (2f - progress);
            return MathHelper.Lerp(32f, NovaDamageRadius, pop);
        }

        Rectangle SlabBody(int side)
        {
            int left = (int)(Projectile.Center.X + side * CurrentSlabOffset - SlabWidth * 0.5f);
            int top = (int)(SlabBottom - HandHeight);
            return new Rectangle(left, top, (int)SlabWidth, (int)HandHeight);
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            // Broadphase covers both slabs at max separation and the 600px clap detonation diameter.
            Projectile.width = (int)(HandStartOffset * 2f + SlabWidth + 16f);
            Projectile.height = (int)Math.Max(NovaDamageRadius * 2f, HandHeight * 2f);
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TotalTelegraphTicks + BurstVisualTicks;
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.5f }, Projectile.Center);
            }
        }

        public override bool? CanDamage()
        {
            // Damage stops once the primary shockwave finishes expanding at tick 24; trailing
            // golden clouds and flare rays over ticks 25-46 are decorative so wisps never clip late.
            if (SlabsConverging)
            {
                return true;
            }

            if (Clapping && ClapAge <= BurstDamageTicks)
            {
                return true;
            }

            return false;
        }

        public override bool CanHitPlayer(Player target)
        {
            if (Clapping)
            {
                return !burstHitPlayers[target.whoAmI];
            }

            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Clapping)
            {
                burstHitPlayers[target.whoAmI] = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Clapping)
            {
                if (ClapAge > BurstDamageTicks)
                {
                    return false;
                }

                float radius = CurrentBurstRadius();
                Vector2 closest = new Vector2(
                    MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                    MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
                float distance = Vector2.Distance(Projectile.Center, closest);
                return distance <= radius;
            }

            // While converging, the monolith sprites themselves own contact damage.
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
            burstEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasSolarBurst", AssetRequestMode.ImmediateLoad);
            monolithTexture ??= ModContent.Request<Texture2D>(TextureRoot + "Particles/GigasConsecratedMonolith", AssetRequestMode.ImmediateLoad);
            flowNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/SmoothNoise", AssetRequestMode.ImmediateLoad);
            crackNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/Vein_02-512x512", AssetRequestMode.ImmediateLoad);
            billowNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/T_NKQ443", AssetRequestMode.ImmediateLoad);
            shockNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/Turbulence_07-512x512", AssetRequestMode.ImmediateLoad);
            sparkNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/Vein_04-512x512", AssetRequestMode.ImmediateLoad);
            flare616Centered ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/T_VFX_Flare_616_Centered", AssetRequestMode.ImmediateLoad);
            flare666Centered ??= ModContent.Request<Texture2D>(TextureRoot + "Noise/T_VFX_Flare_666_Centered", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            float rise = AppearanceProgress;
            if (rise <= 0f)
            {
                return false;
            }

            float slabFade = 1f;
            if (Clapping)
            {
                slabFade = 1f - MathHelper.Clamp(ClapAge / SlabClapFadeTicks, 0f, 1f);
            }

            if (slabFade > 0.01f)
            {
                DrawSlabs(rise, slabFade);
            }

            if (Clapping)
            {
                DrawSolarBurst();
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        void DrawSlabs(float rise, float slabFade)
        {
            float progress = MovementProgress;
            float offset = MathHelper.Lerp(HandStartOffset, 12f, progress * progress);
            float bottom = SlabBottom;
            float baseOpacity = Clapping ? 0.94f : 0.56f + rise * 0.24f;
            float opacity = baseOpacity * slabFade;
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
                    SpriteEffects flip = side > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    Vector2 drawPos = new Vector2(Projectile.Center.X + side * offset, bottom) - Main.screenPosition;
                    Vector2 auraScale = new Vector2(AuraWidth / monolith.Width, AuraHeight * rise / monolith.Height);
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.EntitySpriteDraw(monolith, drawPos, null, Color.White, 0f,
                        new Vector2(monolith.Width * 0.5f, monolith.Height), auraScale, flip, 0);
                }
            }
            finally
            {
                device.Textures[1] = previousFlow;
                device.SamplerStates[1] = previousFlowSampler;
                device.Textures[2] = previousCrack;
                device.SamplerStates[2] = previousCrackSampler;
            }

            // Crisp point-sampled stone monolith matches SlabBody(side) pixel-for-pixel.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Color slabTint = new Color(255, 234, 177, (int)(112f * slabFade));
            Vector2 slabScale = new Vector2(SlabWidth / monolith.Width, HandHeight * rise / monolith.Height);
            for (int side = -1; side <= 1; side += 2)
            {
                SpriteEffects flip = side > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 drawPos = new Vector2(Projectile.Center.X + side * offset, bottom) - Main.screenPosition;
                Main.EntitySpriteDraw(monolith, drawPos, null, slabTint, 0f,
                    new Vector2(monolith.Width * 0.5f, monolith.Height), slabScale, flip, 0);
            }
        }

        void DrawSolarBurst()
        {
            float globalProgress = MathHelper.Clamp(ClapAge / BurstVisualTicks, 0f, 1f);
            float time = Main.GlobalTimeWrappedHourly;

            // Layer 1: three staggered golden plasma ruptures, drawn outermost-first so the hot
            // primary core stays crisp above its billowing aftershocks.
            float[] delays = { 0.00f, 0.12f, 0.24f };
            float[] durations = { 0.62f, 0.66f, 0.72f };
            float[] scales = { 0.90f, 1.00f, 1.08f };
            float[] rotations = { 0.15f, 1.22f, 2.38f };
            float[] phases = { 0.17f, 0.53f, 0.89f };
            float[] opacities = { 0.90f, 0.82f, 0.68f };
            Color[] darkTiers = { new Color(214, 118, 12), new Color(196, 94, 8), new Color(228, 134, 18) };
            Color[] midTiers = { new Color(255, 198, 36), new Color(255, 174, 22), new Color(255, 210, 56) };
            Color[] coreTiers = { new Color(255, 252, 212), new Color(255, 246, 188), new Color(255, 252, 222) };

            for (int pulse = 2; pulse >= 0; pulse--)
            {
                float pulseProgress = (globalProgress - delays[pulse]) / durations[pulse];
                if (pulseProgress <= 0f || pulseProgress >= 1f)
                {
                    continue;
                }

                float pop = pulseProgress * (2f - pulseProgress);
                float fade = MathHelper.Clamp((1f - pulseProgress) * 2.1f, 0f, 1f);
                DrawBurstPass("GigasSolarRupture", billowNoise.Value, shockNoise.Value,
                    new Vector2(740f), rotations[pulse], darkTiers[pulse], midTiers[pulse], coreTiers[pulse],
                    opacities[pulse] * fade, pop, 1f / scales[pulse], phases[pulse], time,
                    BlendState.AlphaBlend, SamplerState.LinearWrap);
            }

            // Layer 2: dual staggered concussion shockwaves. The primary front drives the real
            // circular damage hitbox; the trailing echo wave adds explosion rhythm and depth.
            if (ClapAge <= BurstDamageTicks)
            {
                float shockProgress = MathHelper.Clamp(ClapAge / BurstDamageTicks, 0f, 1f);
                float radius = CurrentBurstRadius();
                float halfThickness = 24f;
                float padding = halfThickness * 4.5f;
                float quadRadius = radius + padding;
                float diameter = quadRadius * 2f;
                float fade = MathHelper.Clamp((1f - shockProgress) * 2.5f, 0f, 1f);

                DrawBurstPass("GigasSolarShockwave", shockNoise.Value, sparkNoise.Value,
                    new Vector2(diameter), 0f, new Color(140, 62, 6), new Color(255, 190, 32), new Color(255, 252, 214),
                    0.95f * fade, shockProgress, radius / quadRadius, diameter / halfThickness, time,
                    BlendState.AlphaBlend, SamplerState.LinearWrap);
            }

            float echoAge = ClapAge - 8f;
            if (echoAge > 0f && echoAge < 32f)
            {
                float echoProgress = echoAge / 32f;
                float echoPop = echoProgress * (2f - echoProgress);
                float echoRadius = MathHelper.Lerp(28f, NovaDamageRadius * 1.05f, echoPop);
                float echoHalfThickness = 18f;
                float echoPadding = echoHalfThickness * 4.5f;
                float echoQuadRadius = echoRadius + echoPadding;
                float echoDiameter = echoQuadRadius * 2f;
                float echoFade = MathHelper.Clamp((1f - echoProgress) * 2.1f, 0f, 1f);

                DrawBurstPass("GigasSolarShockwave", shockNoise.Value, sparkNoise.Value,
                    new Vector2(echoDiameter), 1.57f, new Color(168, 78, 10), new Color(255, 206, 48), new Color(255, 252, 224),
                    0.68f * echoFade, echoProgress, echoRadius / echoQuadRadius, echoDiameter / echoHalfThickness, time,
                    BlendState.AlphaBlend, SamplerState.LinearWrap);
            }

            // Layer 3: high-contrast incandescent sun-heart flash framed by golden occlusion.
            if (ClapAge < 26f)
            {
                float coreProgress = ClapAge / 26f;
                float corePop = coreProgress * (2f - coreProgress);
                float coreDiameter = MathHelper.Lerp(260f, 460f, corePop);
                float coreFade = MathHelper.Clamp((1f - coreProgress) * 1.75f, 0f, 1f);

                DrawBurstPass("GigasSolarCoreFlash", flare616Centered.Value, flowNoise.Value,
                    new Vector2(coreDiameter), 0f, new Color(184, 92, 10), new Color(255, 202, 46), new Color(255, 252, 228),
                    0.96f * coreFade, coreProgress, 1f, 0f, time,
                    BlendState.AlphaBlend, SamplerState.LinearClamp);
            }

            // Layer 4: counter-rotating heavenly starburst pop combining centered Flare_666 (long
            // asymmetric lances piercing past the shock rim) and Flare_616 (8-point cathedral star).
            if (ClapAge < 38f)
            {
                float lanceProgress = ClapAge / 38f;
                float lancePop = lanceProgress * (2f - lanceProgress);
                float lanceScaleUV = MathHelper.Lerp(1.85f, 0.96f, lancePop);
                float lanceFade = MathHelper.Clamp((1f - lanceProgress) * 1.65f, 0f, 1f);
                float lanceRotation = 0.12f + lanceProgress * 0.24f;

                DrawBurstPass("GigasSolarFlareBurst", flare666Centered.Value, shockNoise.Value,
                    new Vector2(860f), lanceRotation, new Color(220, 126, 14), new Color(255, 214, 72), new Color(255, 253, 228),
                    0.95f * lanceFade, lanceProgress, lanceScaleUV, 0.62f, time,
                    BlendState.Additive, SamplerState.LinearClamp);
            }

            if (ClapAge < 30f)
            {
                float starProgress = ClapAge / 30f;
                float starPop = starProgress * (2f - starProgress);
                float starScaleUV = MathHelper.Lerp(1.65f, 1.00f, starPop);
                float starFade = MathHelper.Clamp((1f - starProgress) * 1.80f, 0f, 1f);
                float starRotation = -0.08f - starProgress * 0.20f;

                DrawBurstPass("GigasSolarFlareBurst", flare616Centered.Value, shockNoise.Value,
                    new Vector2(640f), starRotation, new Color(236, 152, 24), new Color(255, 228, 108), new Color(255, 254, 236),
                    0.92f * starFade, starProgress, starScaleUV, 0.15f, time,
                    BlendState.Additive, SamplerState.LinearClamp);
            }
        }

        void DrawBurstPass(string techniqueName, Texture2D primary, Texture2D detail, Vector2 drawSize,
            float rotation, Color darkColor, Color midColor, Color coreColor, float opacity, float progress,
            float active, float direction, float time, BlendState blendState, SamplerState primarySampler)
        {
            if (opacity <= 0.005f)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, primarySampler,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousDetail = device.Textures[1];
            SamplerState previousDetailSampler = device.SamplerStates[1];
            try
            {
                device.Textures[1] = detail;
                device.SamplerStates[1] = SamplerState.LinearWrap;

                Effect effect = burstEffect.Value;
                effect.CurrentTechnique = effect.Techniques[techniqueName];
                effect.Parameters["DarkColor"].SetValue(darkColor.ToVector3());
                effect.Parameters["MidColor"].SetValue(midColor.ToVector3());
                effect.Parameters["CoreColor"].SetValue(coreColor.ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(time);
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Active"].SetValue(active);
                effect.Parameters["Direction"].SetValue(direction);

                Vector2 blockCount = Vector2.Max(drawSize * 0.5f, Vector2.One);
                effect.Parameters["PixelGrid"].SetValue(
                    new Vector4(blockCount.X, blockCount.Y, 1f / blockCount.X, 1f / blockCount.Y));
                effect.CurrentTechnique.Passes[0].Apply();

                Vector2 scale = drawSize / primary.Size();
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White,
                    rotation, primary.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
            finally
            {
                device.Textures[1] = previousDetail;
                device.SamplerStates[1] = previousDetailSampler;
            }
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (!Clapping)
            {
                // For 60 ticks, gold motes gather at the future slab positions while the slabs stay
                // invisible. They then rise in over 18 ticks, hold for 60, and converge to clap.
                float progress = MovementProgress;
                float rise = AppearanceProgress;
                float offset = MathHelper.Lerp(HandStartOffset, 12f, progress * progress);
                float bottom = SlabBottom;
                if (!Main.dedServ)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float x = Projectile.Center.X + side * offset;
                        int dustCount = rise > 0f ? 3 : 1;
                        for (int i = 0; i < dustCount; i++)
                        {
                            float y = bottom - Main.rand.NextFloat(HandHeight * Math.Max(rise, 0.18f));
                            Dust flame = Dust.NewDustDirect(new Vector2(x - 8f, y), 16, 4, DustID.GoldFlame,
                                0f, 0f, 90, default, 1.25f);
                            flame.noGravity = true;
                            flame.velocity = rise > 0f
                                ? new Vector2(-side * 0.4f, -0.8f)
                                : new Vector2(-side * 0.18f, -1.25f);
                        }

                        if (rise > 0f && Main.rand.NextBool(2))
                        {
                            float y = bottom - HandHeight * rise;
                            Dust sparkle = Dust.NewDustDirect(new Vector2(x - 10f, y), 20, 6, DustID.GoldCoin,
                                0f, -1f, 0, default, 1f);
                            sparkle.noGravity = true;
                        }
                    }

                    Lighting.AddLight(Projectile.Center, 0.5f * rise, 0.45f * rise, 0.2f * rise);

                    if (Projectile.localAI[0] >= TotalTelegraphTicks)
                    {
                        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.85f, Pitch = 0.15f }, Projectile.Center);
                        SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.45f, Pitch = 0.25f }, Projectile.Center);
                        UsefulFunctions.ScreenShake(Projectile.Center, 8f, 16);
                    }
                }

                return;
            }

            if (Main.dedServ)
            {
                return;
            }

            if (Projectile.localAI[0] == TotalTelegraphTicks + 1)
            {
                SpawnImpactBurst();
            }

            // Expanding shockwave dust ring during the active burst window; tapers off smoothly.
            if (ClapAge <= BurstDamageTicks)
            {
                float radius = CurrentBurstRadius();
                for (int i = 0; i < 16; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 radial = angle.ToRotationVector2();
                    Vector2 pos = Projectile.Center + radial * (radius + Main.rand.NextFloat(-12f, 12f));
                    Dust flame = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                        radial * Main.rand.NextFloat(2.4f, 5.2f), 60, default, Main.rand.NextFloat(0.85f, 1.45f));
                    flame.noGravity = true;
                }

                for (int i = 0; i < 6; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 radial = angle.ToRotationVector2();
                    Vector2 pos = Projectile.Center + radial * Main.rand.NextFloat(radius * 0.4f, radius);
                    Dust glint = Dust.NewDustPerfect(pos, DustID.AncientLight,
                        radial * Main.rand.NextFloat(3.5f, 7.5f), 20, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.55f, 0.90f));
                    glint.noGravity = true;
                }
            }

            float lightFade = MathHelper.Clamp(1f - ClapAge / BurstVisualTicks, 0f, 1f);
            Lighting.AddLight(Projectile.Center, 1.45f * lightFade, 1.25f * lightFade, 0.55f * lightFade);
        }

        void SpawnImpactBurst()
        {
            // Three distinct velocity/scale layers (all max scale <= 1.74 after Terraria's +-20% jitter)
            // so the detonation pops with bright leading sparks, dense golden flame, and lingering glints.
            for (int i = 0; i < 48; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust flame = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 28f),
                    DustID.GoldFlame,
                    direction * Main.rand.NextFloat(3.5f, 9.5f),
                    55, default, Main.rand.NextFloat(0.80f, 1.45f));
                flame.noGravity = true;
            }

            for (int i = 0; i < 24; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust coin = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 22f),
                    DustID.GoldCoin,
                    direction * Main.rand.NextFloat(5.0f, 11.5f),
                    0, default, Main.rand.NextFloat(0.70f, 1.10f));
                coin.noGravity = true;
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 16f),
                    DustID.AncientLight,
                    direction * Main.rand.NextFloat(7.0f, 14.0f),
                    20, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.55f, 0.90f));
                spark.noGravity = true;
            }
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Red Knight presentation built from the same pixel-measured effect routes used by Gwyn.
    /// Every shader draw supplies a real pixel rectangle rather than scaling a white mask into geometry.
    /// Small held-object accents deliberately use sprites and dust instead of radial shaders.
    /// </summary>
    internal static class RedKnightVFX
    {
        const string EffectRoot = "tsorcRevamp/Effects/";
        const string NoiseRoot = "tsorcRevamp/Textures/Noise/";
        const string ParticleRoot = "tsorcRevamp/Textures/Particles/";

        static Asset<Effect> cinderTrailEffect;
        static Asset<Effect> crimsonEffect;
        static Asset<Effect> dominionEffect;
        static Asset<Effect> destinedDeathEffect;
        static Asset<Texture2D> auraNoise;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> turbulentNoise;
        static Asset<Texture2D> grainNoise;
        static Asset<Texture2D> marbleNoise;
        static Asset<Texture2D> veinNoise;
        static Asset<Texture2D> blobNoise;
        static Asset<Texture2D> billowNoise;
        static Asset<Texture2D> flare;
        static Asset<Texture2D> flameOne;
        static Asset<Texture2D> flameTwo;
        static Asset<Texture2D> flameThree;
        static Asset<Texture2D> muzzleTwo;

        static readonly Color CrimsonCinder = new(38, 1, 5);
        static readonly Color CrimsonFlame = new(208, 22, 28);
        static readonly Color CrimsonCore = new(255, 112, 48);
        static readonly Color GreatCinder = new(48, 1, 3);
        static readonly Color GreatFlame = new(244, 42, 25);
        static readonly Color GreatCore = new(255, 164, 78);
        // Destined Death palette. DarkColor is very nearly black on purpose — under premultiplied
        // alpha that is what lets the sooty half of the flame OCCLUDE the background and read as
        // genuine black fire rather than as a dark tint (vfx-shader-tips §43).
        static readonly Color DestinedSoot = new(6, 0, 3);
        static readonly Color DestinedFlame = new(198, 14, 30);
        static readonly Color DestinedCore = new(255, 132, 86);
        static readonly Color BoltSoot = new(26, 0, 6);
        static readonly Color BoltFlame = new(206, 16, 34);
        static readonly Color BoltCore = new(255, 138, 110);
        static readonly Color PoisonCinder = new(15, 25, 2);
        static readonly Color PoisonBody = new(118, 174, 12);
        static readonly Color PoisonCore = new(226, 255, 102);

        static void LoadAssets()
        {
            if (Main.dedServ)
            {
                return;
            }

            cinderTrailEffect ??= ModContent.Request<Effect>(EffectRoot + "GwynCinderTrail", AssetRequestMode.ImmediateLoad);
            crimsonEffect ??= ModContent.Request<Effect>(EffectRoot + "RedKnightCrimsonVFX", AssetRequestMode.ImmediateLoad);
            dominionEffect ??= ModContent.Request<Effect>(EffectRoot + "GreatRedKnightDominion", AssetRequestMode.ImmediateLoad);
            destinedDeathEffect ??= ModContent.Request<Effect>(EffectRoot + "RedKnightDestinedDeath", AssetRequestMode.ImmediateLoad);
            auraNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            turbulentNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "Turbulence_05-512x512", AssetRequestMode.ImmediateLoad);
            grainNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "Grainy_07-512x512", AssetRequestMode.ImmediateLoad);
            marbleNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_MarbleNoise_tiled", AssetRequestMode.ImmediateLoad);
            veinNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "Vein_04-512x512", AssetRequestMode.ImmediateLoad);
            // Destined Death sampler pair. Both were picked by LOOKING (preview/ContactSheet.ps1)
            // and both survived the -Tile2x2 seam pass, which every scrolling shader needs (§44).
            blobNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Noise_6Yu1", AssetRequestMode.ImmediateLoad);
            billowNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "Turbulence_07-512x512", AssetRequestMode.ImmediateLoad);
            flare ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
            flameOne ??= ModContent.Request<Texture2D>(ParticleRoot + "flame_01_a", AssetRequestMode.ImmediateLoad);
            flameTwo ??= ModContent.Request<Texture2D>(ParticleRoot + "flame_02_a", AssetRequestMode.ImmediateLoad);
            flameThree ??= ModContent.Request<Texture2D>(ParticleRoot + "flame_03_a", AssetRequestMode.ImmediateLoad);
            muzzleTwo ??= ModContent.Request<Texture2D>(ParticleRoot + "muzzle_02_a", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawSpearWake(Vector2 center, float rotation, Vector2 size, float opacity, bool empowered)
        {
            Vector2 wakeSize = new(Math.Min(size.X, 82f), Math.Min(size.Y, 18f));
            DrawCrimsonQuad("RedKnightSpearWake", center, rotation, wakeSize,
                Math.Min(opacity, empowered ? 0.58f : 0.48f),
                new Color(20, 23, 28),
                empowered ? new Color(176, 188, 202) : new Color(142, 151, 162),
                empowered ? new Color(248, 252, 255) : new Color(224, 231, 238),
                0.45f, empowered ? 0.92f : 0.72f, 1f, 1f);
        }

        /// <summary>
        /// The travelling flame shockwave a planted standard throws out. Was RedKnightGroundMiasma,
        /// a flat quad whose only animation was the noise sliding sideways; it is now the Destined
        /// Death black+red flame, two overlapping instances at different phases and speeds so the
        /// wave churns instead of translating rigidly.
        /// </summary>
        internal static void DrawGroundWave(Vector2 groundPoint, Vector2 velocity, Vector2 size, float opacity)
        {
            int direction = velocity.X < 0f ? -1 : 1;
            float width = Math.Max(size.X, 40f) * 1.9f;
            float height = Math.Max(size.Y, 16f) * 2.9f;
            // Two passes: a wide low body and a narrower taller crest slightly ahead of it. Their
            // phases differ so they never sample the same noise and read as one flat stamp (§33).
            DrawDestinedDeathFlame(groundPoint, new Vector2(width, height * 0.72f),
                0.55f, opacity * 0.9f, 0.9f, direction * 1.7f);
            DrawDestinedDeathFlame(groundPoint + new Vector2(direction * width * 0.16f, 0f),
                new Vector2(width * 0.62f, height), 0.7f, opacity, 1.15f, direction * -0.8f);
        }

        internal static void DrawStandardCharge(Vector2 groundPoint, float progress, KnightStandardMode mode)
        {
            bool empowered = mode != KnightStandardMode.RedKnight;
            float width = MathHelper.Lerp(46f, empowered ? 128f : 100f, progress);
            float height = MathHelper.Lerp(30f, empowered ? 96f : 76f, progress);
            float opacity = MathHelper.Lerp(0.22f, empowered ? 0.98f : 0.86f, progress);

            // Charge ramps the flame up in place: `progress` also drives the shader's own envelope,
            // so the pillar grows AND its noise intensifies as the standard nears its release.
            DrawDestinedDeathFlame(groundPoint, new Vector2(width, height),
                MathHelper.Clamp(progress * 0.82f, 0f, 1f), opacity,
                empowered ? 1.2f : 0.95f, (int)mode * 1.3f);
            DrawDestinedDeathFlame(groundPoint, new Vector2(width * 0.5f, height * 1.24f),
                MathHelper.Clamp(progress * 0.9f, 0f, 1f), opacity * 0.85f,
                empowered ? 1.35f : 1.05f, (int)mode * 1.3f + 2.4f);
        }

        /// <summary>Emit a simple flame dust from the bomb sprite's fixed top anchor.</summary>
        internal static void DrawBombFuse(Vector2 fusePoint, float progress, bool planted)
        {
            if (Main.dedServ || !Main.rand.NextBool(planted ? 2 : 3))
            {
                return;
            }

            Dust spark = Dust.NewDustPerfect(fusePoint, DustID.Torch,
                new Vector2(Main.rand.NextFloat(-0.25f, 0.25f), Main.rand.NextFloat(-0.9f, -0.45f)),
                40, default, MathHelper.Lerp(0.65f, 0.9f, progress));
            spark.noGravity = true;
            spark.fadeIn = 0.25f;
        }

        /// <summary>
        /// The standard red lightning lane. Was routing into Gwyn's SunlightJudgment column tinted
        /// storm-BLUE, which is why Stormbreaker Edict read as a different boss's attack; it now
        /// draws the family's own crimson bolt technique.
        /// The widths are much larger than the old 15/8 — at a 620px lane those made the bolt one
        /// pixel of jitter wide, and none of the lightning shape was visible.
        /// </summary>
        internal static void DrawLightningLane(Vector2 start, Vector2 velocity, float length,
            float progress, bool active, float fade, float activeWidth = 46f, float phase = 0f)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            DrawCrimsonBolt(start, direction, length, active ? activeWidth : activeWidth * 0.6f,
                progress, active, fade, phase);
        }

        internal static void DrawCrimsonDominion(Vector2 center, int age, float baseRotation,
            int rotationDirection)
        {
            LoadAssets();
            if (Main.dedServ || dominionEffect == null)
            {
                return;
            }

            const float arenaRadius = CrimsonDominionController.Radius;
            float coverage = 2f * (arenaRadius + Math.Max(Main.screenWidth, Main.screenHeight));
            float radiusRatio = arenaRadius / coverage;
            float fieldOpacity;
            float edgeOpacity;

            if (age < CrimsonDominionController.BuildTicks)
            {
                float build = MathHelper.Clamp(age / (float)CrimsonDominionController.BuildTicks, 0f, 1f);
                fieldOpacity = MathHelper.Lerp(0f, 1f, build);
                edgeOpacity = MathHelper.Lerp(0.08f, 0.95f, build);
                DrawDestinedDeathBlast(center, MathHelper.Lerp(96f, 188f, build),
                    1f - build * 0.5f, 0.30f * build);
            }
            else if (age < CrimsonDominionController.EscapeStart)
            {
                // Fully opaque black wall once containment locks in — no more see-through arena
                // boundary; the red energy waves (DominionEdge, additive) render on top of it.
                fieldOpacity = 1f;
                edgeOpacity = 1f;
            }
            else
            {
                float escape = MathHelper.Clamp((age - CrimsonDominionController.EscapeStart) /
                    (float)CrimsonDominionController.EscapeTicks, 0f, 1f);
                fieldOpacity = 1f - escape;
                edgeOpacity = MathHelper.Lerp(0.9f, 0.22f, escape);
            }

            if (age < CrimsonDominionController.NovaStart)
            {
                DrawDominionQuad("DominionField", center, Vector2.One * coverage, 0f,
                    fieldOpacity, 1f, 0f, 0.88f, radiusRatio, rotationDirection, BlendState.NonPremultiplied);
                DrawDominionQuad("DominionEdge", center, Vector2.One * coverage, 0f,
                    edgeOpacity, 1f, 0f, 1.08f, radiusRatio, rotationDirection, BlendState.Additive);
            }

            // The finishing "get out of the circle" sequence. Replaces the old DominionNova pair,
            // which peaked at 0.88 alpha of a mostly-transparent field and read as a faded wash.
            // Now: a black Destined Death seal FILLS the arena over SealFillTicks, its crimson
            // energy intensifying toward the advancing edge, then detonates.
            //
            // The seal deliberately starts LATE in the escape window rather than at EscapeStart.
            // The escape window itself is a balance number (it is how long the player has to reach
            // the boundary) and is left alone; only the visual is retimed, so the whole readable
            // "fill then blast" beat lands in roughly two seconds as intended.
            if (age >= CrimsonDominionController.SealStart && age < CrimsonDominionController.NovaStart)
            {
                float fill = MathHelper.Clamp((age - CrimsonDominionController.SealStart) /
                    (float)CrimsonDominionController.SealFillTicks, 0f, 1f);
                DrawDestinedDeathSeal(center, arenaRadius * 2f, fill,
                    MathHelper.Lerp(0.45f, 1f, fill));
            }
            else if (age >= CrimsonDominionController.NovaStart)
            {
                // One continuous blast curve across nova + fade so the explosion keeps expanding
                // while it dissipates instead of snapping to a second animation.
                float blast = MathHelper.Clamp((age - CrimsonDominionController.NovaStart) /
                    (float)(CrimsonDominionController.NovaTicks + CrimsonDominionController.FadeTicks),
                    0f, 1f);
                float fade = age < CrimsonDominionController.FadeStart ? 1f
                    : MathHelper.Clamp(1f - (age - CrimsonDominionController.FadeStart) /
                        (float)CrimsonDominionController.FadeTicks, 0f, 1f);
                DrawDestinedDeathBlast(center, arenaRadius * 2f, blast, fade);
                // A second, slightly larger and hotter pass for the first instant only — this is
                // the flash, and it is the one place additive stacking is honestly wanted.
                if (age < CrimsonDominionController.FadeStart)
                {
                    DrawDestinedDeathBlast(center, arenaRadius * 2.16f, blast * 0.7f, fade * 0.55f);
                }
            }
        }

        /// <summary>
        /// Crimson Dominion's body engulf: the knight stands holding its spear wrapped in the same
        /// black-and-crimson Destined Death flame the seal detonates with. Anchored on the sprite's
        /// bottom edge (feet), not its centre, so the flame sits on the ground it is standing on.
        /// </summary>
        /// <param name="front">false = the heavy pass drawn behind the sprite (PreDraw); true = the
        /// thin pass drawn after the sprite and its held spear (PostDraw), so the flame wraps the
        /// knight rather than only silhouetting behind it.</param>
        internal static void DrawDominionEngulf(Vector2 feet, float scale, float opacity, bool front)
        {
            if (opacity <= 0f)
            {
                return;
            }
            if (front)
            {
                DrawDestinedDeathFlame(feet + new Vector2(-6f * scale, 0f),
                    new Vector2(58f, 118f) * scale, 0.58f, opacity, 1.25f, 5.1f);
                return;
            }
            DrawDestinedDeathFlame(feet, new Vector2(96f, 108f) * scale, 0.5f, opacity, 1.15f, 0f);
            DrawDestinedDeathFlame(feet + new Vector2(-14f * scale, 0f),
                new Vector2(52f, 138f) * scale, 0.62f, opacity * 0.8f, 1.3f, 2.9f);
            DrawDestinedDeathFlame(feet + new Vector2(15f * scale, 0f),
                new Vector2(46f, 124f) * scale, 0.44f, opacity * 0.8f, 1.25f, -3.4f);
        }

        internal static void DrawToxicMotes(Vector2 center, int count, float progress, float radius)
        {
            DrawPoisonOrb(center, Vector2.Zero, 0.72f + progress * 0.18f,
                0.86f + progress * 0.18f);

            if (Main.dedServ || !Main.rand.NextBool(4))
            {
                return;
            }

            Dust mote = Dust.NewDustPerfect(
                center + Main.rand.NextVector2Circular(Math.Min(radius, 8f), Math.Min(radius, 8f)),
                DustID.CursedTorch, Main.rand.NextVector2Circular(0.22f, 0.22f), 120,
                new Color(135, 196, 24), Main.rand.NextFloat(0.55f, 0.8f));
            mote.noGravity = true;
        }

        internal static void DrawPoisonOrb(Vector2 center, Vector2 velocity, float opacity, float scale = 1f)
        {
            bool moving = velocity.LengthSquared() > 0.04f;
            Vector2 direction = moving ? velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitX;
            Vector2 size = Vector2.One * 30f * scale * (moving ? 0.9f : 1f);
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            DrawCrimsonQuad("RedKnightPoisonOrb", center, 0f, size, opacity * pulse,
                PoisonCinder, PoisonBody, PoisonCore, 0.72f, 0.8f,
                direction.X < 0f ? -1f : 1f, 0f);
        }

        internal static void DrawUltrakillSeal(Vector2 center, float progress)
        {
            float eased = progress * progress * (3f - 2f * progress);
            float gatherRadius = MathHelper.Lerp(128f, 34f, eased);
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f);

            // Black swirling shadow behind the gather so the pink glow reads against darkness
            // instead of hovering transparent over the arena background.
            DrawVoidGather(center, new Vector2(320f, 260f),
                (0.7f + progress * 0.2f) * pulse, new Color(10, 0, 8), new Color(90, 6, 20));

            DrawCrimsonQuad("RedKnightUltrakillGather", center, 0f,
                new Vector2(270f, 205f), (0.62f + progress * 0.18f) * pulse,
                new Color(22, 0, 22), new Color(176, 10, 46), new Color(255, 118, 92),
                progress, 1.1f, 1f, 0f);

            for (int i = 0; i < 3; i++)
            {
                float angle = i switch
                {
                    0 => -0.18f,
                    1 => 2.18f,
                    _ => 4.42f
                };
                Vector2 outward = angle.ToRotationVector2();
                Vector2 inward = -outward;
                DrawCrimsonQuad("RedKnightSpearWake", center + outward * gatherRadius * 0.52f,
                    inward.ToRotation(), new Vector2(Math.Max(26f, gatherRadius * 0.92f), 11f + i * 2f),
                    (0.24f + progress * 0.16f) * pulse,
                    GreatCinder, GreatFlame, GreatCore, progress, 0.72f, 1f, 1f);
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 dustPosition = center + Main.rand.NextVector2CircularEdge(gatherRadius, gatherRadius * 0.72f);
                Dust mote = Dust.NewDustPerfect(dustPosition,
                    Main.rand.NextBool(4) ? DustID.Shadowflame : DustID.RedTorch,
                    (center - dustPosition).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.2f, 2.1f),
                    120, new Color(208, 28, 56), Main.rand.NextFloat(0.65f, 0.95f));
                mote.noGravity = true;
            }
        }

        internal static void DrawHerald(Vector2 center, float progress, bool storm)
        {
            float envelope = (float)Math.Sin(progress * MathHelper.Pi);

            if (storm)
            {
                // Complete redo. Was a blue/cyan gather (DrawVoidGather + six Gwyn judgment strands
                // + a storm-blue radial band) that read as a different boss's attack and as a flat
                // ring. It is now one dedicated crimson technique: a dark occluding storm body with
                // branching crimson discharge crackling through it and a hot heart on the knight.
                float diameter = MathHelper.Lerp(150f, 470f, MathHelper.Clamp(progress * 1.05f, 0f, 1f));
                DrawStormHeraldGather(center, diameter, progress,
                    MathHelper.Clamp(0.35f + envelope * 0.65f, 0f, 1f));

                if (!Main.dedServ && envelope > 0.08f && Main.rand.NextBool(2))
                {
                    Vector2 dustPosition = center + Main.rand.NextVector2CircularEdge(
                        diameter * 0.34f, diameter * 0.34f);
                    Dust arc = Dust.NewDustPerfect(dustPosition,
                        Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.RedTorch,
                        (center - dustPosition).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.4f, 3f),
                        110, new Color(206, 16, 34), Main.rand.NextFloat(0.7f, 1.15f));
                    arc.noGravity = true;
                }
            }
            else
            {
                Vector2 furnaceBase = center + new Vector2(0f, 30f);
                DrawCrimsonFlameCluster(furnaceBase, progress, envelope * 0.96f,
                    MathHelper.Lerp(1.05f, 1.72f, progress),
                    MathHelper.Lerp(22f, 38f, progress), progress < 0.5f ? -1 : 1);
                if (!Main.dedServ && envelope > 0.08f && Main.rand.NextBool(2))
                {
                    Vector2 dustPosition = furnaceBase + new Vector2(
                        Main.rand.NextFloat(-46f, 46f), Main.rand.NextFloat(-12f, 4f));
                    Dust ember = Dust.NewDustPerfect(dustPosition,
                        Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.RedTorch,
                        new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(-3.2f, -1.1f)),
                        110, new Color(205, 18, 42), Main.rand.NextFloat(0.72f, 1.12f));
                    ember.noGravity = true;
                }
            }
        }

        internal static void DrawBurst(RedKnightBurstKind kind, Vector2 center, float progress, float scale)
        {
            if (kind != RedKnightBurstKind.BombExplosion)
            {
                bool empowered = kind == RedKnightBurstKind.StandardImpact || scale >= 1f;
                float envelope = (1f - progress) * (empowered ? 0.96f : 0.78f);
                DrawCrimsonFlameCluster(center + new Vector2(0f, 8f), progress,
                    envelope, (empowered ? 1.08f : 0.82f) * scale,
                    (empowered ? 18f : 12f) * scale, progress < 0.5f ? -1 : 1);
                return;
            }

            float diameter = 132f * scale;
            DrawCrimsonQuad("RedKnightBombBlast", center, 0f,
                new Vector2(diameter, diameter), 0.96f,
                new Color(30, 0, 12), GreatFlame, GreatCore,
                progress, 1f, 1f, 0f);

            if (progress < 0.16f)
            {
                DrawSmallFlare(center, GreatCore, (1f - progress / 0.16f) * 0.075f * scale);
            }
        }

        static void DrawCrimsonFlameCluster(Vector2 groundPoint, float progress,
            float opacity, float scale, float spread, int direction)
        {
            LoadAssets();
            if (Main.dedServ || flameOne == null || flameTwo == null || flameThree == null
                || muzzleTwo == null || opacity <= 0f)
            {
                return;
            }

            direction = direction < 0 ? -1 : 1;
            float growth = MathHelper.Lerp(0.62f, 1.12f,
                MathHelper.Clamp(progress, 0f, 1f));
            SpriteEffects facing = direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            DrawCrimsonFlameTongue(flameTwo.Value,
                groundPoint + new Vector2(-spread, 0f), new Vector2(40f, 58f) * scale * growth,
                -0.09f * direction, opacity * 0.68f, progress, 0.86f, direction,
                facing, BlendState.NonPremultiplied);
            DrawCrimsonFlameTongue(flameThree.Value,
                groundPoint + new Vector2(spread, 1f), new Vector2(35f, 52f) * scale * growth,
                0.11f * direction, opacity * 0.62f, progress, 0.9f, -direction,
                facing, BlendState.NonPremultiplied);
            DrawCrimsonFlameTongue(flameOne.Value,
                groundPoint + new Vector2(-spread * 0.18f, 2f), new Vector2(31f, 49f) * scale * growth,
                -0.035f * direction, opacity * 0.72f, progress, 1.02f, direction,
                facing, BlendState.Additive);
            DrawCrimsonFlameTongue(muzzleTwo.Value,
                groundPoint + new Vector2(spread * 0.18f, 3f), new Vector2(23f, 41f) * scale * growth,
                0.04f * direction, opacity * 0.62f, progress, 1.08f, -direction,
                facing, BlendState.Additive);
        }

        static void DrawDominionQuad(string techniqueName, Vector2 center, Vector2 size, float rotation,
            float opacity, float progress, float active, float intensity, float radiusRatio,
            float direction, BlendState blendState)
        {
            LoadAssets();
            if (Main.dedServ || dominionEffect == null || marbleNoise == null || turbulentNoise == null
                || veinNoise == null || opacity <= 0f || size.X <= 0f || size.Y <= 0f)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture1 = graphicsDevice.Textures[1];
            Texture previousTexture2 = graphicsDevice.Textures[2];
            Texture previousTexture3 = graphicsDevice.Textures[3];
            SamplerState previousSampler1 = graphicsDevice.SamplerStates[1];
            SamplerState previousSampler2 = graphicsDevice.SamplerStates[2];
            SamplerState previousSampler3 = graphicsDevice.SamplerStates[3];
            try
            {
                graphicsDevice.Textures[1] = marbleNoise.Value;
                graphicsDevice.Textures[2] = turbulentNoise.Value;
                graphicsDevice.Textures[3] = veinNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[3] = SamplerState.LinearWrap;

                Effect effect = dominionEffect.Value;
                effect.CurrentTechnique = effect.Techniques[techniqueName];
                effect.Parameters["DarkColor"].SetValue(new Color(17, 0, 14).ToVector3());
                effect.Parameters["MidColor"].SetValue(new Color(178, 9, 48).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 122, 108).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Intensity"].SetValue(intensity);
                effect.Parameters["Active"].SetValue(active);
                effect.Parameters["RadiusRatio"].SetValue(radiusRatio);
                effect.Parameters["Direction"].SetValue(direction);
                effect.Parameters["DrawSize"]?.SetValue(size);
                effect.CurrentTechnique.Passes[0].Apply();

                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.EntitySpriteDraw(pixel, center - Main.screenPosition, null, Color.White,
                    rotation, pixel.Size() * 0.5f, size / pixel.Size(), SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture1;
                graphicsDevice.Textures[2] = previousTexture2;
                graphicsDevice.Textures[3] = previousTexture3;
                graphicsDevice.SamplerStates[1] = previousSampler1;
                graphicsDevice.SamplerStates[2] = previousSampler2;
                graphicsDevice.SamplerStates[3] = previousSampler3;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        static void DrawCrimsonFlameTongue(Texture2D texture, Vector2 groundPoint,
            Vector2 size, float rotation, float opacity, float progress, float intensity,
            float direction, SpriteEffects effects, BlendState blendState)
        {
            LoadAssets();
            if (Main.dedServ || crimsonEffect == null || turbulentNoise == null || grainNoise == null
                || texture == null || opacity <= 0f || size.X <= 0f || size.Y <= 0f)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture1 = graphicsDevice.Textures[1];
            Texture previousTexture2 = graphicsDevice.Textures[2];
            SamplerState previousSampler1 = graphicsDevice.SamplerStates[1];
            SamplerState previousSampler2 = graphicsDevice.SamplerStates[2];
            try
            {
                graphicsDevice.Textures[1] = turbulentNoise.Value;
                graphicsDevice.Textures[2] = grainNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                Effect effect = crimsonEffect.Value;
                effect.CurrentTechnique = effect.Techniques["RedKnightCrimsonFlame"];
                effect.Parameters["DarkColor"].SetValue(new Color(12, 0, 8).ToVector3());
                effect.Parameters["MidColor"].SetValue(new Color(205, 8, 38).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 104, 68).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Intensity"].SetValue(intensity);
                effect.Parameters["Direction"].SetValue(direction);
                effect.Parameters["TailStrength"].SetValue(0f);
                effect.Parameters["DrawSize"]?.SetValue(size);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(texture, groundPoint - Main.screenPosition, null, Color.White,
                    rotation, new Vector2(texture.Width * 0.5f, texture.Height),
                    size / texture.Size(), effects, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture1;
                graphicsDevice.Textures[2] = previousTexture2;
                graphicsDevice.SamplerStates[1] = previousSampler1;
                graphicsDevice.SamplerStates[2] = previousSampler2;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        static void DrawCrimsonQuad(string techniqueName, Vector2 center, float rotation, Vector2 size,
            float opacity, Color darkColor, Color midColor, Color coreColor,
            float progress, float intensity, float direction, float tailStrength)
        {
            LoadAssets();
            if (Main.dedServ || crimsonEffect == null || turbulentNoise == null || grainNoise == null
                || opacity <= 0f || size.X <= 0f || size.Y <= 0f)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture1 = graphicsDevice.Textures[1];
            Texture previousTexture2 = graphicsDevice.Textures[2];
            SamplerState previousSampler1 = graphicsDevice.SamplerStates[1];
            SamplerState previousSampler2 = graphicsDevice.SamplerStates[2];
            try
            {
                graphicsDevice.Textures[1] = turbulentNoise.Value;
                graphicsDevice.Textures[2] = grainNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                Effect effect = crimsonEffect.Value;
                effect.CurrentTechnique = effect.Techniques[techniqueName];
                effect.Parameters["DarkColor"].SetValue(darkColor.ToVector3());
                effect.Parameters["MidColor"].SetValue(midColor.ToVector3());
                effect.Parameters["CoreColor"].SetValue(coreColor.ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Intensity"].SetValue(intensity);
                effect.Parameters["Direction"].SetValue(direction);
                effect.Parameters["TailStrength"].SetValue(tailStrength);
                effect.Parameters["DrawSize"]?.SetValue(size);
                effect.CurrentTechnique.Passes[0].Apply();

                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.EntitySpriteDraw(pixel, center - Main.screenPosition, null, Color.White,
                    rotation, pixel.Size() * 0.5f, size / pixel.Size(), SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture1;
                graphicsDevice.Textures[2] = previousTexture2;
                graphicsDevice.SamplerStates[1] = previousSampler1;
                graphicsDevice.SamplerStates[2] = previousSampler2;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        /// <summary>Dark swirling shadow drawn with real (non-additive) alpha so it darkens the
        /// background behind an additive glow instead of just adding more light on top of it.</summary>
        internal static void DrawVoidGather(Vector2 center, Vector2 size, float opacity,
            Color darkColor, Color midColor)
        {
            LoadAssets();
            if (Main.dedServ || crimsonEffect == null || turbulentNoise == null
                || opacity <= 0f || size.X <= 0f || size.Y <= 0f)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture1 = graphicsDevice.Textures[1];
            SamplerState previousSampler1 = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = turbulentNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                Effect effect = crimsonEffect.Value;
                effect.CurrentTechnique = effect.Techniques["RedKnightVoidGather"];
                effect.Parameters["DarkColor"].SetValue(darkColor.ToVector3());
                effect.Parameters["MidColor"].SetValue(midColor.ToVector3());
                effect.Parameters["CoreColor"].SetValue(midColor.ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(0f);
                effect.Parameters["Intensity"].SetValue(1f);
                effect.Parameters["Direction"].SetValue(0f);
                effect.Parameters["TailStrength"].SetValue(0f);
                effect.Parameters["DrawSize"]?.SetValue(size);
                effect.CurrentTechnique.Passes[0].Apply();

                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.EntitySpriteDraw(pixel, center - Main.screenPosition, null, Color.White,
                    0f, pixel.Size() * 0.5f, size / pixel.Size(), SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture1;
                graphicsDevice.SamplerStates[1] = previousSampler1;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        static void DrawCinderArc(Vector2 center, float rotation, Vector2 size,
            float opacity, bool empowered, float progress)
        {
            LoadAssets();
            if (Main.dedServ || cinderTrailEffect == null || auraNoise == null || smoothNoise == null || opacity <= 0f)
            {
                return;
            }

            Rectangle source = new(0, 0, Math.Max(2, (int)Math.Ceiling(size.X)), Math.Max(2, (int)Math.Ceiling(size.Y)));
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = smoothNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                Effect effect = cinderTrailEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GwynCinderArc"];
                effect.Parameters["CinderColor"].SetValue((empowered ? GreatCinder : CrimsonCinder).ToVector3());
                effect.Parameters["FlameColor"].SetValue((empowered ? GreatFlame : CrimsonFlame).ToVector3());
                effect.Parameters["CoreColor"].SetValue((empowered ? GreatCore : CrimsonCore).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(auraNoise.Value.Size());
                effect.Parameters["Progress"].SetValue(progress);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(auraNoise.Value, center - Main.screenPosition, source, Color.White,
                    rotation, source.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        /// <summary>
        /// The one entry point for every RedKnightDestinedDeath.fx technique. Always premultiplied
        /// AlphaBlend — the shaders return float4(colour * alpha + emissive, alpha), which is what
        /// makes black flame possible at all (vfx-shader-tips §43). Do NOT call this with Additive.
        /// </summary>
        /// <param name="origin">Normalised anchor inside the quad: (0.5, 0.5) centres it,
        /// (0.5, 1) hangs it off its bottom edge, which is what the bottom-anchored flame wants.</param>
        static void DrawDestinedDeathQuad(string techniqueName, Vector2 anchor, float rotation,
            Vector2 size, Vector2 origin, float opacity, float progress, float intensity,
            float active, float direction, Color darkColor, Color midColor, Color coreColor)
        {
            LoadAssets();
            if (Main.dedServ || destinedDeathEffect == null || blobNoise == null || billowNoise == null
                || opacity <= 0f || size.X <= 0f || size.Y <= 0f)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture1 = graphicsDevice.Textures[1];
            Texture previousTexture2 = graphicsDevice.Textures[2];
            SamplerState previousSampler1 = graphicsDevice.SamplerStates[1];
            SamplerState previousSampler2 = graphicsDevice.SamplerStates[2];
            try
            {
                graphicsDevice.Textures[1] = blobNoise.Value;
                graphicsDevice.Textures[2] = billowNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                Effect effect = destinedDeathEffect.Value;
                effect.CurrentTechnique = effect.Techniques[techniqueName];
                effect.Parameters["DarkColor"].SetValue(darkColor.ToVector3());
                effect.Parameters["MidColor"].SetValue(midColor.ToVector3());
                effect.Parameters["CoreColor"].SetValue(coreColor.ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(MathHelper.Clamp(progress, 0f, 1f));
                effect.Parameters["Intensity"].SetValue(intensity);
                effect.Parameters["Active"].SetValue(active);
                effect.Parameters["Direction"].SetValue(direction);
                effect.Parameters["DrawSize"]?.SetValue(size);
                effect.CurrentTechnique.Passes[0].Apply();

                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.EntitySpriteDraw(pixel, anchor - Main.screenPosition, null, Color.White,
                    rotation, pixel.Size() * origin, size / pixel.Size(), SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture1;
                graphicsDevice.Textures[2] = previousTexture2;
                graphicsDevice.SamplerStates[1] = previousSampler1;
                graphicsDevice.SamplerStates[2] = previousSampler2;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        /// <summary>
        /// Elden-Ring "Destined Death" flame: black fire and crimson fire burning together, anchored
        /// on its bottom edge so <paramref name="groundPoint"/> is the ground line (or the caster's
        /// feet). General purpose — the standard shockwaves, the standard charge and Crimson
        /// Dominion's body engulf are all this one technique at different sizes.
        /// </summary>
        /// <param name="phase">Per-instance sampling offset so adjacent flames are not one stamp
        /// repeated (§33). Any value works; neighbouring callers should differ by ~1.</param>
        internal static void DrawDestinedDeathFlame(Vector2 groundPoint, Vector2 size,
            float progress, float opacity, float intensity = 1f, float phase = 0f)
        {
            DrawDestinedDeathQuad("DestinedDeathFlame", groundPoint, 0f, size,
                new Vector2(0.5f, 1f), opacity, progress, intensity, 0f, phase,
                DestinedSoot, DestinedFlame, DestinedCore);
        }

        /// <summary>Crimson Dominion's finishing circle while it FILLS.</summary>
        internal static void DrawDestinedDeathSeal(Vector2 center, float diameter,
            float progress, float opacity)
        {
            DrawDestinedDeathQuad("DestinedDeathSeal", center, 0f, Vector2.One * diameter,
                Vector2.One * 0.5f, opacity, progress, 1f, 0f, 0f,
                DestinedSoot, DestinedFlame, DestinedCore);
        }

        /// <summary>The detonation the seal culminates in.</summary>
        internal static void DrawDestinedDeathBlast(Vector2 center, float diameter,
            float progress, float opacity)
        {
            DrawDestinedDeathQuad("DestinedDeathBlast", center, 0f, Vector2.One * diameter,
                Vector2.One * 0.5f, opacity, progress, 1f, 0f, 0f,
                DestinedSoot, DestinedFlame, DestinedCore);
        }

        /// <summary>The Storm Herald gather, crimson rather than the old storm-blue.</summary>
        internal static void DrawStormHeraldGather(Vector2 center, float diameter,
            float progress, float opacity)
        {
            DrawDestinedDeathQuad("RedKnightStormHerald", center, 0f, Vector2.One * diameter,
                Vector2.One * 0.5f, opacity, progress, 1f, 0f, 0f,
                DestinedSoot, DestinedFlame, DestinedCore);
        }

        /// <summary>
        /// THE standard Red Knight lightning bolt. Every lightning in the family routes through here
        /// so Stormbreaker Edict and Crimson Dominion cannot drift into two different looks.
        /// </summary>
        static void DrawCrimsonBolt(Vector2 start, Vector2 direction, float length, float width,
            float progress, bool active, float opacity, float phase)
        {
            direction = direction.SafeNormalize(Vector2.UnitX);
            DrawDestinedDeathQuad("RedKnightLightningBolt", start + direction * length * 0.5f,
                direction.ToRotation(), new Vector2(length, width), Vector2.One * 0.5f,
                opacity, progress, 1f, active ? 1f : 0f, phase,
                BoltSoot, BoltFlame, BoltCore);
        }

        static void DrawSmallFlare(Vector2 center, Color color, float scale)
        {
            LoadAssets();
            if (Main.dedServ || flare == null || scale <= 0f)
            {
                return;
            }
            Texture2D texture = flare.Value;
            Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color * 0.5f,
                Main.GlobalTimeWrappedHourly, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}

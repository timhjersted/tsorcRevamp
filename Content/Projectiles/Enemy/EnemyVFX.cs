using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    internal enum EnemyVFXBurstKind
    {
        BlackKnightHexShatter,
        BlackKnightMoonfuryBlast,
        BlackKnightSpearImpact,
        DemonSpiritSoulBurst,
        EvilEyeFlameImpact,
        ElandVenomImpact,
        QuaraWaterBurst,
        QuaraInkBurst,
        EvilEyeGhostBurst,
        EvilEyeTeleportBurst,
        GreatBlackKnightBlightBombBlast,
        MantassinGlintFlash,
        MantassinBlink
    }

    internal static class EnemyVFX
    {
        const string EffectRoot = "tsorcRevamp/Effects/";
        const string NoiseRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> blackKnightHexCrystal;
        static Asset<Effect> blackKnightDeathVolley;
        static Asset<Effect> blackKnightGravefall;
        static Asset<Effect> blackKnightMoonfury;
        static Asset<Effect> blackKnightSpearWake;
        static Asset<Effect> demonSpiritCastSigil;
        static Asset<Effect> demonSpiritSoulComet;
        static Asset<Effect> demonSpiritCrushOrb;
        static Asset<Effect> evilEyeGroundGlyph;
        static Asset<Effect> evilEyeChargeLane;
        static Asset<Effect> evilEyeNovaHalo;
        static Asset<Effect> evilEyeFlameTrail;
        static Asset<Effect> evilEyeDeathBurst;
        static Asset<Effect> evilEyeTeleport;
        static Asset<Effect> elandToxicVFX;
        static Asset<Effect> quaraHydromancyCast;
        static Asset<Effect> quaraWaterProjectile;
        static Asset<Effect> quaraTidalCrest;
        static Asset<Effect> quaraInkGeyser;
        static Asset<Effect> quaraTideRush;
        static Asset<Effect> greatBlackKnightFlail;
        static Asset<Effect> voidExplosion;
        static Asset<Effect> archdeaconGlintstone;
        static Asset<Effect> mantassinGlintstone;
        static Asset<Effect> witchkingVoidCrescent;

        static Asset<Texture2D> circle;
        static Asset<Texture2D> gradient;
        static Asset<Texture2D> spiral;
        static Asset<Texture2D> windstreak;
        static Asset<Texture2D> trail;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> smoke;
        static Asset<Texture2D> flare;
        static Asset<Texture2D> cloudNoise;
        static Asset<Texture2D> voronoiNoise;
        static Asset<Texture2D> swirlyNoise;
        static Asset<Texture2D> perlinTiled;
        static Asset<Texture2D> wavyDetailNoise;
        static Asset<Texture2D> veinNoise;
        static Asset<Texture2D> turbulentNoise;
        static Asset<Texture2D> voidExplosionDetail;
        static Asset<Texture2D> fireExplosion;

        // The Gwyn flipbook begins low in its 216x216 cells, then grows to fill them. These are the
        // centres of each frame's non-transparent bounds relative to the cell centre, in source
        // texels. Removing that baked-in drift keeps the reusable explosion centred on its caller.
        static readonly Vector2[] VoidExplosionFrameCenterOffsets =
        {
            new(0.5f, 33.5f), new(0f, 31f), new(0.5f, 30.5f), new(0.5f, 30f), new(0.5f, 28f),
            new(0.5f, 25.5f), new(1.5f, 23f), new(1.5f, 20.5f), new(2f, 18f), new(2.5f, 16f),
            new(3f, 15f), new(4f, 14f), new(3f, 12.5f), new(2f, 10f), new(1.5f, 8f),
            new(1.5f, 6f), new(1f, 4.5f), new(1f, 3f), new(1f, 1f), new(1f, -0.5f),
            new(1f, -1f), new(0f, -1f), new(0f, -2f), new(0f, -2.5f), new(0f, -2f),
        };

        static readonly Color CurseDark = new(8, 4, 15);
        static readonly Color CurseMid = new(98, 38, 138);
        static readonly Color CurseCore = new(236, 216, 255);
        static readonly Color EyeDark = new(8, 12, 34);
        static readonly Color EyeMid = new(54, 112, 226);
        static readonly Color EyeCore = new(214, 246, 255);
        static readonly Color ToxicDark = new(8, 25, 8);
        static readonly Color ToxicMid = new(66, 176, 54);
        static readonly Color ToxicCore = new(213, 255, 133);
        // Shadow Ninja smoke bombs: charcoal with a faint violet cast, no bright core (it is smoke, not gas).
        static readonly Color ShadowSmokeDark = new(6, 6, 9);
        static readonly Color ShadowSmokeMid = new(46, 44, 54);
        static readonly Color ShadowSmokeCore = new(96, 92, 108);
        static readonly Color WaterDark = new(5, 25, 52);
        static readonly Color WaterMid = new(32, 150, 221);
        static readonly Color WaterCore = new(213, 249, 255);
        static readonly Color InkDark = new(3, 3, 9);
        static readonly Color InkMid = new(30, 25, 67);
        static readonly Color InkCore = new(142, 197, 226);
        // Black plague magic (Black Knight / Great Black Knight). Deliberately its own palette rather
        // than an edit to Curse*, which DemonSpirit shares. The core is ASH, not white: a near-white
        // CoreColor driven additively is why every one of these effects used to read as a white blob.
        static readonly Color PlagueDark = new(10, 6, 16);
        static readonly Color PlagueMid = new(86, 40, 122);
        static readonly Color PlagueCore = new(196, 182, 206);
        static readonly Color BlightDark = new(3, 3, 5);
        static readonly Color BlightMid = new(24, 24, 27);
        static readonly Color BlightCore = new(199, 150, 57);
        // Hex crystal aura: rich black & gold palette (dark abyssal black -> warm ember gold -> soft bright gold core)
        static readonly Color HexGoldDark = new(8, 4, 12);
        static readonly Color HexGoldMid = new(210, 140, 25);
        static readonly Color HexGoldCore = new(255, 215, 110);
        // Great Black Knight: same family, deeper and colder so the two read as related but distinct.
        static readonly Color GreatPlagueDark = new(3, 3, 5);
        static readonly Color GreatPlagueMid = new(22, 21, 25);
        static readonly Color GreatPlagueCore = new(183, 136, 49);
        // Weapon motion (spear wake, flail smear): grey steel-ash with no colour of its own, so a
        // swing reads as displaced air rather than as another magic effect. Replaces the old red
        // Steel* set, which fought the plague theme.
        static readonly Color PlagueAshDark = new(16, 14, 20);
        static readonly Color PlagueAshMid = new(74, 66, 86);
        static readonly Color PlagueAshCore = new(198, 196, 206);

        // Archdeacon glintstone palette: deep azure body / glint blue / blue-white (approved in the offline preview).
        static readonly Color GlintDark = new(20, 50, 140);
        static readonly Color GlintMid = new(70, 145, 255);
        static readonly Color GlintCore = new(225, 245, 255);

        // Mantassin finger-sorcery palette, from the user's Elden Ring reference art: silver light with a faint
        // lavender cast, white core, and the Fleeting Microcosm's gold haze. Its shader has no DarkColor term.
        internal static readonly Color MantassinMid = new(168, 172, 226);
        internal static readonly Color MantassinCore = new(246, 246, 255);
        internal static readonly Color MantassinGold = new(255, 196, 92);
        static readonly Color MantassinBlinkDark = new(10, 10, 26);

        // Witchking's void fire: dark purple frayed edges, bright purple body, white accents (premultiplied, so the dark
        // purple reads over a bright sky too).
        static readonly Color VoidFireDark = new(46, 8, 78);
        static readonly Color VoidFireMid = new(176, 82, 255);
        static readonly Color VoidFireCore = new(255, 246, 255);

        static void LoadAssets()
        {
            if (Main.dedServ)
            {
                return;
            }

            blackKnightHexCrystal ??= ModContent.Request<Effect>(EffectRoot + "BlackKnightHexCrystal", AssetRequestMode.ImmediateLoad);
            blackKnightDeathVolley ??= ModContent.Request<Effect>(EffectRoot + "BlackKnightDeathVolley", AssetRequestMode.ImmediateLoad);
            blackKnightGravefall ??= ModContent.Request<Effect>(EffectRoot + "BlackKnightGravefall", AssetRequestMode.ImmediateLoad);
            blackKnightMoonfury ??= ModContent.Request<Effect>(EffectRoot + "BlackKnightMoonfury", AssetRequestMode.ImmediateLoad);
            blackKnightSpearWake ??= ModContent.Request<Effect>(EffectRoot + "BlackKnightSpearWake", AssetRequestMode.ImmediateLoad);
            demonSpiritCastSigil ??= ModContent.Request<Effect>(EffectRoot + "DemonSpiritCastSigil", AssetRequestMode.ImmediateLoad);
            demonSpiritSoulComet ??= ModContent.Request<Effect>(EffectRoot + "DemonSpiritSoulComet", AssetRequestMode.ImmediateLoad);
            demonSpiritCrushOrb ??= ModContent.Request<Effect>(EffectRoot + "DemonSpiritCrushOrb", AssetRequestMode.ImmediateLoad);
            evilEyeGroundGlyph ??= ModContent.Request<Effect>(EffectRoot + "EvilEyeGroundGlyph", AssetRequestMode.ImmediateLoad);
            evilEyeChargeLane ??= ModContent.Request<Effect>(EffectRoot + "EvilEyeChargeLane", AssetRequestMode.ImmediateLoad);
            evilEyeNovaHalo ??= ModContent.Request<Effect>(EffectRoot + "EvilEyeNovaHalo", AssetRequestMode.ImmediateLoad);
            evilEyeFlameTrail ??= ModContent.Request<Effect>(EffectRoot + "EvilEyeFlameTrail", AssetRequestMode.ImmediateLoad);
            evilEyeDeathBurst ??= ModContent.Request<Effect>(EffectRoot + "EvilEyeDeathBurst", AssetRequestMode.ImmediateLoad);
            evilEyeTeleport ??= ModContent.Request<Effect>(EffectRoot + "EvilEyeTeleport", AssetRequestMode.ImmediateLoad);
            elandToxicVFX ??= ModContent.Request<Effect>(EffectRoot + "ElandToxicVFX", AssetRequestMode.ImmediateLoad);
            quaraHydromancyCast ??= ModContent.Request<Effect>(EffectRoot + "QuaraHydromancyCast", AssetRequestMode.ImmediateLoad);
            quaraWaterProjectile ??= ModContent.Request<Effect>(EffectRoot + "QuaraWaterProjectile", AssetRequestMode.ImmediateLoad);
            quaraTidalCrest ??= ModContent.Request<Effect>(EffectRoot + "QuaraTidalCrest", AssetRequestMode.ImmediateLoad);
            quaraInkGeyser ??= ModContent.Request<Effect>(EffectRoot + "QuaraInkGeyser", AssetRequestMode.ImmediateLoad);
            quaraTideRush ??= ModContent.Request<Effect>(EffectRoot + "QuaraTideRush", AssetRequestMode.ImmediateLoad);
            greatBlackKnightFlail ??= ModContent.Request<Effect>(EffectRoot + "GreatBlackKnightFlail", AssetRequestMode.ImmediateLoad);
            voidExplosion ??= ModContent.Request<Effect>(EffectRoot + "VoidExplosion", AssetRequestMode.ImmediateLoad);
            archdeaconGlintstone ??= ModContent.Request<Effect>(EffectRoot + "ArchdeaconGlintstone", AssetRequestMode.ImmediateLoad);
            mantassinGlintstone ??= ModContent.Request<Effect>(EffectRoot + "MantassinGlintstone", AssetRequestMode.ImmediateLoad);
            witchkingVoidCrescent ??= ModContent.Request<Effect>(EffectRoot + "WitchkingVoidCrescent", AssetRequestMode.ImmediateLoad);

            circle ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_CircleFit1", AssetRequestMode.ImmediateLoad);
            gradient ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Gradient_circle22", AssetRequestMode.ImmediateLoad);
            spiral ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Spiral07", AssetRequestMode.ImmediateLoad);
            windstreak ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Windstreak3", AssetRequestMode.ImmediateLoad);
            trail ??= ModContent.Request<Texture2D>(NoiseRoot + "T_trail12", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            smoke ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_RoundSmoke71", AssetRequestMode.ImmediateLoad);
            flare ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
            // Eland's poison kit: billowing tileable cloud for the gas body, voronoi cells for the
            // corrosive bubbling. Both are seamless, so they tile cleanly at any sample scale.
            cloudNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_CloudNoise_Tiled", AssetRequestMode.ImmediateLoad);
            voronoiNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "Voronoi_10-512x512", AssetRequestMode.ImmediateLoad);
            // EvilEye's blink: wispy swirls over a slow seamless underlayer. Both were unused
            // mod-wide, deliberately picked so the portal doesn't reuse the same broken-noise look
            // that already appears on three other effects.
            swirlyNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "SwirlyNoise", AssetRequestMode.ImmediateLoad);
            perlinTiled ??= ModContent.Request<Texture2D>(NoiseRoot + "T_PerlinNoise_Tiled", AssetRequestMode.ImmediateLoad);
            wavyDetailNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Noise_Wf4", AssetRequestMode.ImmediateLoad);
            // Black plague kit. Vein_07 is a reticulated cell web that reads as diseased tissue;
            // Turbulence_05 is dark with bright filaments, for detonation churn. Both are seamless,
            // so they tile cleanly at any sample scale. These replace T_Windstreak3 and T_trail12,
            // which were wired as SHAPE sources for six lane/trail techniques and are the wrong
            // shapes entirely: the "windstreak" is a vertical teardrop blob (hence the hard white
            // lozenges) and "trail12" is a small centred 4-point star flare.
            veinNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "Vein_07-512x512", AssetRequestMode.ImmediateLoad);
            turbulentNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "Turbulence_05-512x512", AssetRequestMode.ImmediateLoad);
            // Higher-resolution turbulence makes the shader's world-space 2x2 UV quantisation
            // visible instead of letting an upscaled 216px flipbook cell hide it.
            voidExplosionDetail ??= ModContent.Request<Texture2D>(NoiseRoot + "TurbulentNoise", AssetRequestMode.ImmediateLoad);
            // Gwyn's Descent of the Sun explosion sheet. VoidExplosion samples only the selected
            // frame's alpha as a reusable cloud silhouette, then supplies its own palette/material.
            fireExplosion ??= ModContent.Request<Texture2D>(NoiseRoot + "T_fire_flipbook4_sm", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawBlackKnightHexCrystal(Vector2 center, Vector2 velocity, float dormantProgress, bool active)
        {
            LoadAssets();
            if (active)
            {
                Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
                Draw(blackKnightHexCrystal, "BlackKnightHexComet", veinNoise, cloudNoise,
                    center - direction * 36f, new Vector2(96f, 30f), direction.ToRotation(),
                    HexGoldDark, HexGoldMid, HexGoldCore, 0.82f, dormantProgress, 1f, 1f);
            }
            // Seal technique = "Corona" (black flame). The original smooth-ring "BlackKnightHexSeal"
            // and the alternate "BlackKnightHexSealVoid" (a tight bright halo around a deeper void)
            // both remain compiled in the .xnb — swapping this string is the whole change, no other
            // edit needed. Void is being held for another effect.
            //
            // Note this helper also backs the sub-1/3-health crystal storm overlay on both knights
            // and GreatBlackKnight's homing telegraph, so all four uses move together — intentional,
            // they are one visual family.
            Draw(blackKnightHexCrystal, "BlackKnightHexSealCorona", perlinTiled, veinNoise,
                center, active ? new Vector2(48f, 40f) : Vector2.One * 74f, 0f,
                HexGoldDark, HexGoldMid, HexGoldCore, active ? 0.9f : 0.74f,
                dormantProgress, active ? 1f : 0f, 1f);
        }

        /// <summary>
        /// The Curse Ward plane: the black-flame seal drawn as a tall OVAL standing in front of the knight.
        /// </summary>
        /// <remarks>
        /// Reuses "BlackKnightHexSealCorona" completely unchanged. That technique measures
        /// <c>r = length(c - 0.5)</c> in UV space, which is a circle in UV but an ELLIPSE on screen as soon
        /// as the quad is non-square — so an oval needs a draw size, not a new shader. Its pixel grid comes
        /// from <c>drawSize</c> too, so the 2px blocks stay square whatever the aspect.
        ///
        /// Plague palette rather than the crystal's gold, and `progress` runs BACKWARDS relative to the
        /// crystal's use of it: the technique tightens its ring as progress rises, so feeding it 1 -> 0
        /// makes the ward bloom outward as it comes up.
        /// </remarks>
        internal static void DrawBlackKnightCurseWard(Vector2 center, int facing, float rise, float opacity)
        {
            LoadAssets();
            // Grows to full height as it rises so it reads as a plane being raised, not popped into place.
            Vector2 size = new Vector2(54f, MathHelper.Lerp(46f, 112f, rise));
            Draw(blackKnightHexCrystal, "BlackKnightHexSealCorona", perlinTiled, veinNoise,
                center + new Vector2(facing * 26f, -2f), size, 0f,
                PlagueDark, PlagueMid, PlagueCore, opacity,
                1f - rise, 1f, facing);
        }

        internal static void DrawBlackKnightDeathSeal(Vector2 center, float progress, bool blight = false)
        {
            LoadAssets();
            float size = MathHelper.Lerp(500f, 96f, progress);
            Draw(blackKnightDeathVolley, "BlackKnightDeathSeal", perlinTiled, veinNoise,
                center, Vector2.One * size, 0f, blight ? BlightDark : PlagueDark,
                blight ? BlightMid : PlagueMid, blight ? BlightCore : PlagueCore,
                0.78f, progress, 1f, 1f);
        }

        internal static void DrawBlackKnightAimThread(Vector2 start, Vector2 end, float progress, bool blight = false)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 2f)
            {
                return;
            }
            LoadAssets();
            // 12px was too thin to carry a halo now that the thread is procedural rather than a
            // stretched star flare; 18px gives the soft outer glow somewhere to live.
            Draw(blackKnightDeathVolley, "BlackKnightAimThread", wavyDetailNoise, turbulentNoise,
                Vector2.Lerp(start, end, 0.5f), new Vector2(length, 18f), delta.ToRotation(),
                blight ? BlightDark : PlagueDark, blight ? BlightMid : PlagueMid,
                blight ? BlightCore : PlagueCore, 0.62f, progress, 1f, 1f);
        }

        /// <summary>The Witchking's Void Flame: a ")" crescent of purple fire, bulge leading, on a quad rotated to the
        /// velocity. <paramref name="size"/> = (quad width along travel, crescent height). The band's middle half-thickness
        /// (px) goes to the shader as a fraction of the quad width (Active), pre-divided here to save ps_2_0 slots.
        /// <paramref name="phase"/> offsets the noise so a fan of crescents doesn't burn identically.</summary>
        internal static void DrawWitchkingVoidCrescent(Vector2 center, Vector2 size, float rotation, float midHalfThickness, float opacity, float phase)
        {
            LoadAssets();
            float activeHalfThickness = midHalfThickness / MathHelper.Max(size.X, 1f);
            Draw(witchkingVoidCrescent, "WitchkingVoidCrescent", cloudNoise, perlinTiled,
                center, size, rotation, VoidFireDark, VoidFireMid, VoidFireCore,
                opacity, 0f, activeHalfThickness, phase, BlendState.AlphaBlend);
        }

        internal static void DrawBlackKnightDeathTrail(Vector2 center, Vector2 velocity, Vector2 size, float opacity, bool blight = false)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            LoadAssets();
            Draw(blackKnightDeathVolley, "BlackKnightDeathTrail", veinNoise, cloudNoise,
                center - direction * size.X * 0.28f, size, direction.ToRotation(),
                blight ? BlightDark : PlagueDark, blight ? BlightMid : PlagueMid,
                blight ? BlightCore : PlagueCore, opacity, 0.5f, 1f, 1f);
        }

        internal static void DrawBlackKnightGraveTear(Vector2 center, float progress)
        {
            LoadAssets();
            Draw(blackKnightGravefall, "BlackKnightGraveTear", wavyDetailNoise, cloudNoise,
                center, new Vector2(104f, 42f), 0f, PlagueDark, PlagueMid, PlagueCore,
                0.88f, progress, 0f, 1f);
        }

        internal static void DrawBlackKnightGraveTrail(Vector2 center, Vector2 velocity)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitY);
            LoadAssets();
            // Rot cloud, so it occludes rather than glows.
            Draw(blackKnightGravefall, "BlackKnightGraveTrail", veinNoise, cloudNoise,
                center - direction * 32f, new Vector2(86f, 22f), direction.ToRotation(),
                PlagueDark, PlagueMid, PlagueCore, 0.68f, 0.5f, 1f, 1f, BlendState.AlphaBlend);
        }

        /// <summary>
        /// Red fuse sparks for a held Moonfury bomb. Replaces the Moonfury shader that used to be
        /// drawn on the held-bomb telegraph and sat visibly offset from the sprite — dust needs no
        /// quad-to-sprite alignment to read correctly, so it cannot drift out of place.
        /// </summary>
        internal static void SpawnBombFuseSparks(Vector2 fuseWorld, float fuseProgress)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Ramps as the fuse burns down, so the telegraph still says "about to go off".
            int count = 1 + (int)(fuseProgress * 2f);
            for (int i = 0; i < count; i++)
            {
                Dust spark = Dust.NewDustPerfect(
                    fuseWorld + Main.rand.NextVector2Circular(3f, 3f), DustID.RedTorch,
                    new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-1.8f, -0.8f)),
                    0, default, MathHelper.Lerp(0.9f, 1.5f, fuseProgress));
                spark.noGravity = true;
                spark.fadeIn = 0.4f;
            }
            Lighting.AddLight(fuseWorld, 0.75f * (0.4f + fuseProgress), 0.10f, 0.04f);
        }

        internal static void DrawBlackKnightMoonfury(Vector2 center, Vector2 velocity, float progress, bool active, bool blight = false)
        {
            LoadAssets();
            // The thrown bomb's smoke trail was removed on request — it read as a flat purple box.
            // (Root cause was the premultiplied-alpha bug now fixed in the shader, but the trail
            // added little over the bomb's own glow, so it stays gone. The explosion still draws
            // its own smoke via DrawBurst.)
            // The bomb sprite is 34x34 and the orb now carries a bloom out past r=0.86, so the quad
            // has to be comfortably larger than the sprite or the bloom has nowhere to spill.
            Draw(blackKnightMoonfury, "BlackKnightMoonfuryCoal", wavyDetailNoise, veinNoise,
                center, Vector2.One * (active ? 82f : 58f), 0f,
                blight ? BlightDark : PlagueDark, blight ? BlightMid : PlagueMid,
                blight ? BlightCore : PlagueCore, 0.88f, progress, active ? 1f : 0f, 1f);
        }

        internal static void DrawBlackKnightSpearWake(Vector2 center, float rotation, Vector2 size, float opacity)
        {
            LoadAssets();
            // Wind, not a glow: alpha-blended grey-ash so a swing reads as displaced air. Additive
            // was half of why this looked like a solid white lozenge against a bright sky.
            Draw(blackKnightSpearWake, "BlackKnightSpearWake", perlinTiled, cloudNoise,
                center, size, rotation, PlagueAshDark, PlagueAshMid, PlagueAshCore,
                opacity, 0.5f, 1f, 1f, BlendState.AlphaBlend);
        }

        // Archdeacon glintstone sorcery (Effects/ArchdeaconGlintstone.fx). All four are premultiplied AlphaBlend over a
        // procedural quad (cloud noise drawn, broken noise in s1), 2px pixel filter.
        // Comet: the head sits at uv.x 0.80 of a 3:1 quad pointing along `rotation`; tailLength 0 = bare glint.
        internal static void DrawArchdeaconGlintComet(Vector2 headCenter, float rotation, float height, float tailLength, float opacity)
        {
            LoadAssets();
            Vector2 size = new Vector2(height * 3f, height);
            // Shift the quad back along the travel direction so the head, not the quad centre, sits on headCenter.
            Vector2 quadCenter = headCenter - rotation.ToRotationVector2() * (size.X * 0.30f);
            Draw(archdeaconGlintstone, "ArchdeaconGlintComet", cloudNoise, brokenNoise,
                quadCenter, size, rotation, GlintDark, GlintMid, GlintCore,
                opacity, MathHelper.Clamp(tailLength, 0f, 1f), 1f, 1f, BlendState.AlphaBlend);
        }

        // Orb: sphere radius is 0.52 of half the quad, the ribbons reach 0.82.
        internal static void DrawArchdeaconGlintOrb(Vector2 center, float diameter, float opacity)
        {
            LoadAssets();
            Draw(archdeaconGlintstone, "ArchdeaconGlintOrb", cloudNoise, brokenNoise,
                center, new Vector2(diameter), 0f, GlintDark, GlintMid, GlintCore,
                opacity, 0f, 1f, 1f, BlendState.AlphaBlend);
        }

        // Snare field: a square quad whose floor line is 92% of the way down it. `pulse` 0..1 flashes the field bright.
        internal static void DrawArchdeaconGlintSnare(Vector2 floorCenter, float width, float opacity, float pulse)
        {
            LoadAssets();
            Vector2 size = new Vector2(width, width);
            Vector2 quadCenter = floorCenter + new Vector2(0f, size.Y * (0.5f - 0.92f));
            Draw(archdeaconGlintstone, "ArchdeaconGlintSnare", cloudNoise, brokenNoise,
                quadCenter, size, 0f, GlintDark, GlintMid, GlintCore,
                opacity, MathHelper.Clamp(pulse, 0f, 1f), 1f, 1f, BlendState.AlphaBlend);
        }

        // Lance: drawn on the lance hitbox, tip at the top.
        internal static void DrawArchdeaconGlintLance(Vector2 center, Vector2 size, float opacity)
        {
            LoadAssets();
            Draw(archdeaconGlintstone, "ArchdeaconGlintLance", cloudNoise, brokenNoise,
                center, size, 0f, GlintDark, GlintMid, GlintCore,
                opacity, 0f, 1f, 1f, BlendState.AlphaBlend);
        }

        // Mantassin finger sorceries (Effects/MantassinGlintstone.fx). Premultiplied AlphaBlend, 2px pixel filter.
        // Glint: the 4-point star over a nail head. `flare` 0..1 brightens it (pre-launch flare, shatter flash).
        internal static void DrawMantassinNailGlint(Vector2 center, float diameter, float opacity, float flare)
        {
            LoadAssets();
            Draw(mantassinGlintstone, "MantassinNailGlint", cloudNoise, brokenNoise,
                center, new Vector2(diameter), 0f, Color.Black, MantassinMid, MantassinCore,
                opacity, MathHelper.Clamp(flare, 0f, 1f), 1f, 1f, BlendState.AlphaBlend);
        }

        // Fleeting Microcosm. The quad is the hit diameter / 0.70 (the shader puts the hit radius at r = 0.70).
        // `open` 0..1 = how far the black void has opened, `flash` = the rim flash on the pulse.
        // The two crossed lens-flare spikes are drawn first so the disc's black interior covers their middles.
        internal static void DrawMantassinMicrocosm(Vector2 center, float hitRadius, float open, float flash,
            float opacity, float flareRotation, float flareLength)
        {
            LoadAssets();
            Vector2 raySize = new Vector2(flareLength, flareLength * 0.07f);
            float rayOpacity = opacity * MathHelper.Clamp(open, 0f, 1f);
            Draw(mantassinGlintstone, "MantassinMicrocosmRay", cloudNoise, brokenNoise,
                center, raySize, flareRotation, Color.Black, MantassinMid, MantassinCore,
                rayOpacity, 0f, 1f, 1f, BlendState.AlphaBlend, MantassinGold);
            Draw(mantassinGlintstone, "MantassinMicrocosmRay", cloudNoise, brokenNoise,
                center, raySize, flareRotation + MathHelper.PiOver2, Color.Black, MantassinMid, MantassinCore,
                rayOpacity, 0f, 1f, 1f, BlendState.AlphaBlend, MantassinGold);

            float quadSize = hitRadius * 2f / 0.70f;
            Draw(mantassinGlintstone, "MantassinMicrocosm", cloudNoise, brokenNoise,
                center, new Vector2(quadSize), 0f, Color.Black, MantassinMid, MantassinCore,
                opacity, MathHelper.Clamp(open, 0f, 1f), flash, 1f, BlendState.AlphaBlend, MantassinGold);
        }

        // Cherishing Light arc. The shader draws 70 degrees of a circle at r = 0.75 of the half-quad, centred on +x,
        // so the quad is sized from the arc radius and rotated to the arc's middle angle.
        // `trace` 0..1 draws it in from one tip; `flash` lights it up on a block.
        internal static void DrawMantassinCrescent(Vector2 center, float arcRadius, float middleAngle, float trace,
            float flash, float opacity)
        {
            LoadAssets();
            float quadSize = arcRadius * 2f / 0.75f;
            Draw(mantassinGlintstone, "MantassinCrescent", cloudNoise, brokenNoise,
                center, new Vector2(quadSize), middleAngle, Color.Black, MantassinMid, MantassinCore,
                opacity, MathHelper.Clamp(trace, 0f, 1f), flash, 1f, BlendState.AlphaBlend);
        }

        // Floor sigil: a flat ellipse lying on the floor. `countdown` 0..1 = how much of its timer has run.
        internal static void DrawMantassinRune(Vector2 floorCenter, float width, float countdown, float opacity)
        {
            LoadAssets();
            Vector2 size = new Vector2(width, width * 0.30f);
            Draw(mantassinGlintstone, "MantassinRune", cloudNoise, brokenNoise,
                floorCenter, size, 0f, Color.Black, MantassinMid, MantassinCore,
                opacity, MathHelper.Clamp(countdown, 0f, 1f), 1f, 1f, BlendState.AlphaBlend);
        }

        // Nail trail: a primitive strip along the nail's recorded path, points[0] = the head. Not a SpriteBatch quad,
        // because the trail has to bend round tile bounces. Vertex alpha fades 1 at the head -> 0 at the tail end;
        // the shader tapers the width. PixelGrid comes from the strip's real length so the blocks stay 2px.
        internal static void DrawMantassinNailTrail(Vector2[] points, int count, float width, float opacity)
        {
            if (Main.dedServ || count < 2 || opacity <= 0f)
            {
                return;
            }
            LoadAssets();

            float totalLength = 0f;
            for (int i = 1; i < count; i++)
            {
                totalLength += Vector2.Distance(points[i], points[i - 1]);
            }
            if (totalLength < 2f)
            {
                return;
            }

            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[count * 2];
            short[] indices = new short[(count - 1) * 6];
            float travelled = 0f;

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    travelled += Vector2.Distance(points[i], points[i - 1]);
                }

                Vector2 tangent;
                if (i == 0)
                {
                    tangent = points[0] - points[1];
                }
                else
                {
                    tangent = points[i - 1] - points[i];
                }
                if (tangent.LengthSquared() < 0.001f)
                {
                    tangent = Vector2.UnitX;
                }
                tangent.Normalize();

                Vector2 normal = new Vector2(-tangent.Y, tangent.X);
                float along = travelled / totalLength;
                Color vertexColor = Color.White * (1f - along);
                Vector2 screenPoint = points[i] - Main.screenPosition;
                Vector2 halfWidth = normal * (width * 0.5f);

                vertices[i * 2] = new VertexPositionColorTexture(new Vector3(screenPoint - halfWidth, 0f), vertexColor, new Vector2(along, 0f));
                vertices[i * 2 + 1] = new VertexPositionColorTexture(new Vector3(screenPoint + halfWidth, 0f), vertexColor, new Vector2(along, 1f));
            }

            for (int i = 0; i < count - 1; i++)
            {
                int vertex = i * 2;
                int index = i * 6;
                indices[index] = (short)vertex;
                indices[index + 1] = (short)(vertex + 1);
                indices[index + 2] = (short)(vertex + 2);
                indices[index + 3] = (short)(vertex + 2);
                indices[index + 4] = (short)(vertex + 1);
                indices[index + 5] = (short)(vertex + 3);
            }

            Effect effect = mantassinGlintstone.Value;
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Main.spriteBatch.End();

            BlendState previousBlend = device.BlendState;
            RasterizerState previousRasterizer = device.RasterizerState;
            Texture previousTexture = device.Textures[0];
            SamplerState previousSampler = device.SamplerStates[0];

            try
            {
                device.BlendState = BlendState.AlphaBlend;
                device.RasterizerState = RasterizerState.CullNone;
                device.Textures[0] = cloudNoise.Value;
                device.SamplerStates[0] = SamplerState.LinearWrap;

                // Pre-divided pixel grid (see Draw): 2px blocks along the trail's real length and across its width.
                Vector2 pixelBlocks = Vector2.Max(new Vector2(totalLength, width) / 2f, Vector2.One);

                effect.CurrentTechnique = effect.Techniques["MantassinNailTrail"];
                effect.Parameters["WorldViewProjection"]?.SetValue(global::tsorcRevamp.Content.Projectiles.VFX.DynamicTrail.GetWorldViewProjectionMatrix());
                effect.Parameters["MidColor"]?.SetValue(MantassinMid.ToVector3());
                effect.Parameters["CoreColor"]?.SetValue(MantassinCore.ToVector3());
                effect.Parameters["Opacity"]?.SetValue(opacity);
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["PixelGrid"]?.SetValue(
                    new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y));
                effect.CurrentTechnique.Passes[0].Apply();

                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length,
                    indices, 0, indices.Length / 3);
            }
            finally
            {
                device.BlendState = previousBlend;
                device.RasterizerState = previousRasterizer;
                device.Textures[0] = previousTexture;
                device.SamplerStates[0] = previousSampler;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }

        internal static void DrawDemonSpiritCastSigil(Vector2 center, int pattern, float progress, Vector2 aimDirection)
        {
            LoadAssets();
            Draw(demonSpiritCastSigil, "DemonSpiritCastSigil", circle, brokenNoise,
                center, Vector2.One * 126f, 0f, CurseDark, new Color(138, 52, 181), CurseCore,
                0.86f, progress, pattern, 1f);

            if (pattern <= 2)
            {
                aimDirection = aimDirection.SafeNormalize(Vector2.UnitX);
                // Ray fan width by pattern: 0 = single bolt, 1 = wide trident, anything else = a pair.
                int rayCount = 2;

                if (pattern == 0)
                {
                    rayCount = 1;
                }
                else if (pattern == 1)
                {
                    rayCount = 3;
                }
                for (int i = 0; i < rayCount; i++)
                {
                    float spread = rayCount == 1 ? 0f : MathHelper.Lerp(-0.28f, 0.28f, i / (float)(rayCount - 1));
                    Vector2 rayDirection = aimDirection.RotatedBy(spread);
                    Draw(demonSpiritCastSigil, "DemonSpiritCastRay", trail, smoothNoise,
                        center + rayDirection * 48f, new Vector2(104f, 10f), rayDirection.ToRotation(),
                        CurseDark, new Color(138, 52, 181), CurseCore, 0.58f,
                        progress, pattern, 1f);
                }
            }
            else
            {
                int enclosureCount = pattern - 2;
                for (int i = 1; i < enclosureCount; i++)
                {
                    float size = 126f - i * 25f;
                    Draw(demonSpiritCastSigil, "DemonSpiritCastSigil", circle, brokenNoise,
                        center, Vector2.One * size, i * 0.35f, CurseDark,
                        new Color(138, 52, 181), CurseCore, 0.52f, progress, pattern, 1f);
                }
            }
        }

        internal static void DrawDemonSpiritSoulComet(Vector2 center, Vector2 velocity, float expiryProgress)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            LoadAssets();
            // Primary was T_Aurax44 — a discrete wisp graphic, mostly black canvas, not a tileable
            // turbulence field, so scrolling through it as "noise" sampled black almost everywhere
            // and the trail was a barely-visible scribble. turbulentNoise (Turbulence_05, sampled
            // twice at independent scale/phase inside the shader) drives the body now; veinNoise
            // (Vein_07) is the sparkle-glint detail layer. Both already loaded for the Black Knight
            // kit, so this needed no new asset.
            Draw(demonSpiritSoulComet, "DemonSpiritSoulComet", turbulentNoise, veinNoise,
                center - direction * 42f, new Vector2(112f, 42f), direction.ToRotation(),
                CurseDark, CurseMid, CurseCore, 0.78f, expiryProgress, 1f, 1f);
            if (expiryProgress > 0f)
            {
                Draw(demonSpiritSoulComet, "DemonSpiritExpiryBrackets", circle, smoothNoise,
                    center, Vector2.One * 214f, 0f, CurseDark, CurseMid, CurseCore,
                    0.72f * expiryProgress, expiryProgress, 1f, 1f);
            }
        }

        internal static void DrawDemonSpiritCrush(Vector2 center, Vector2 velocity)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            LoadAssets();
            Draw(demonSpiritCrushOrb, "DemonSpiritCrushRibbon", trail, smoothNoise,
                center - direction * 25f, new Vector2(64f, 20f), direction.ToRotation(),
                CurseDark, new Color(184, 42, 177), CurseCore, 0.62f, 0.5f, 1f, 1f);
            Draw(demonSpiritCrushOrb, "DemonSpiritCrushOrb", spiral, brokenNoise,
                center, Vector2.One * 28f, 0f, CurseDark, new Color(202, 46, 190), CurseCore,
                0.9f, 0.5f, 1f, 1f);
        }

        internal static void DrawEvilEyeGroundGlyph(Vector2 center, float progress, bool active)
        {
            LoadAssets();
            Draw(evilEyeGroundGlyph, "EvilEyeGroundGlyph", circle, brokenNoise,
                center, Vector2.One * 70f, 0f, EyeDark, EyeMid, EyeCore,
                active ? 1f : 0.78f, progress, active ? 1f : 0f, 1f);
        }

        internal static void DrawEvilEyeCharge(Vector2 center, Vector2 direction, float progress, bool active, bool enraged)
        {
            direction = direction.SafeNormalize(Vector2.UnitX);
            LoadAssets();
            Color mid = enraged ? Color.Lerp(EyeMid, Color.OrangeRed, 0.42f) : EyeMid;
            if (!active)
            {
                Draw(evilEyeChargeLane, "EvilEyeChargeLane", trail, smoothNoise,
                    center + direction * 330f, new Vector2(700f, 10f), direction.ToRotation(),
                    EyeDark, mid, EyeCore, 0.42f, progress, 0f, 1f);
            }
            Draw(evilEyeChargeLane, "EvilEyeChargeAperture", circle, brokenNoise,
                center, new Vector2(87f, 130f), direction.ToRotation(),
                EyeDark, mid, EyeCore, 0.9f, progress, active ? 1f : 0f, 1f);
            if (active)
            {
                Draw(evilEyeChargeLane, "EvilEyeChargeWake", windstreak, brokenNoise,
                    center - direction * 54f, new Vector2(146f, 126f), direction.ToRotation(),
                    EyeDark, mid, EyeCore, 0.72f, progress, 1f, 1f);
            }
        }

        internal static void DrawEvilEyeNova(Vector2 center, float progress, bool enragedTransition)
        {
            LoadAssets();
            Draw(evilEyeNovaHalo, "EvilEyeNovaHalo", circle, brokenNoise,
                center, Vector2.One * 152f, 0f, EyeDark, EyeMid, EyeCore,
                0.88f, progress, enragedTransition ? 1f : 0f, 1f);
        }

        internal static void DrawEvilEyeFlame(Vector2 center, Vector2 velocity, bool seeking)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            LoadAssets();
            // Longer and taller than before so the squiggle and the bell-curve width have room to
            // actually be visible; the tail is procedural now so the primary texture is only a quad.
            Draw(evilEyeFlameTrail, "EvilEyeFlameTail", windstreak, smoothNoise,
                center - direction * 40f, new Vector2(96f, 30f), direction.ToRotation(),
                EyeDark, EyeMid, EyeCore, 0.85f, 0.5f, seeking ? 1f : 0f, 1f);
            // 18f -> 34f: the sprite is 16px, so the old orb was entirely hidden behind it. At this
            // size the shader's halo spills past the sprite's silhouette and reads as a glow around it.
            Draw(evilEyeFlameTrail, "EvilEyeFlameCore", gradient, smoothNoise,
                center, Vector2.One * 34f, 0f, EyeDark, EyeMid, EyeCore,
                0.95f, 0.5f, seeking ? 1f : 0f, 1f);
        }

        /// <summary>
        /// How much larger than the damaging radius the gas cloud is drawn. The shader keeps the fog
        /// dense out to the real damage edge and feathers away inside this extra margin, so the cloud
        /// has no visible border - the trade is that the visible gas extends past what actually hurts.
        /// </summary>
        const float FogVisualScale = 1.3f;

        /// <param name="rotation">
        /// Per-instance quad rotation. The shader samples its noise in local UV space, so without
        /// this every puff of a trail samples the identical pattern and they read as the same stamp
        /// repeated. Rotating the quad rotates the sampled noise, making each puff visually distinct
        /// for free (the fog is radially symmetric, so nothing else is affected).
        /// </param>
        internal static void DrawElandToxicField(Vector2 center, Vector2 size, float progress, bool active,
            float rotation = 0f, float visualScale = FogVisualScale, float opacity = -1f)
        {
            LoadAssets();
            // Direction carries the damage-radius ratio (see ElandToxicVFX.fx ToxicField) - it is no
            // longer a circular/box flag. Every poison cloud is round now; the old box path is what
            // rendered those hard white squares on screen.
            // A negative opacity means "caller did not specify" -> fall back to the active/idle defaults.
            float resolvedOpacity = opacity;

            if (resolvedOpacity < 0f)
            {
                resolvedOpacity = active ? 0.78f : 0.68f;
            }

            Draw(elandToxicVFX, "ElandToxicField", voronoiNoise, cloudNoise,
                center, size * visualScale, rotation, ToxicDark, ToxicMid, ToxicCore,
                resolvedOpacity, progress, active ? 1f : 0f, 1f / visualScale,
                BlendState.AlphaBlend);
        }

        /// <summary>Shadow Ninja smoke cloud: the ToxicField fog (round, noise-edged, no border) in the charcoal palette.
        /// Same progress hold curve: full from 0.11 to 0.72, faded out at 1.0.</summary>
        internal static void DrawShadowSmokeField(Vector2 center, Vector2 size, float progress, float rotation, float opacity)
        {
            LoadAssets();
            Draw(elandToxicVFX, "ElandToxicField", voronoiNoise, cloudNoise,
                center, size * FogVisualScale, rotation, ShadowSmokeDark, ShadowSmokeMid, ShadowSmokeCore,
                opacity, progress, 0f, 1f / FogVisualScale, BlendState.AlphaBlend);
        }

        /// <summary>
        /// Soft, centred aura for Eland's spinning PoisonSmog projectile. Keep this separate from
        /// the deliberately asymmetric ToxicField used by the large fog attacks: that irregular
        /// silhouette looks right for ground gas but reads as an offset when wrapped around a core.
        /// </summary>
        internal static void DrawElandPoisonBurstAura(Vector2 center, float progress)
        {
            LoadAssets();
            Draw(elandToxicVFX, "ElandPoisonBurstAura", voronoiNoise, cloudNoise,
                center, Vector2.One * 38f, 0f, ToxicDark, ToxicMid, ToxicCore,
                0.66f, progress, 1f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawElandVenomProjectile(Vector2 center, Vector2 velocity, Vector2 size, float opacity = 0.8f)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            LoadAssets();
            Draw(elandToxicVFX, "ElandVenomGlob", voronoiNoise, cloudNoise,
                center - direction * size.X * 0.28f, size, direction.ToRotation(),
                ToxicDark, ToxicMid, ToxicCore, opacity, 0.5f, 1f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawQuaraCast(Vector2 center, float progress, int pattern)
        {
            LoadAssets();
            Color mid = pattern == 3 ? new Color(28, 42, 78) : WaterMid;
            Color core = pattern == 3 ? InkCore : WaterCore;
            string technique = pattern switch
            {
                0 => "QuaraBarrageCast",
                1 => "QuaraCrestCast",
                2 => "QuaraBubbleCast",
                3 => "QuaraInkCast",
                _ => "QuaraRushCast"
            };
            Draw(quaraHydromancyCast, technique, circle, swirlyNoise,
                center, Vector2.One * (58f + pattern * 5f), pattern * 0.22f,
                WaterDark, mid, core, 0.76f, progress, pattern, 1f);
        }

        internal static void DrawQuaraBubble(Vector2 center, Vector2 size, float progress, bool pressurized)
        {
            LoadAssets();
            Draw(quaraWaterProjectile, "QuaraBubble", circle, smoothNoise,
                center, size, 0f, WaterDark, WaterMid, WaterCore,
                0.88f, progress, pressurized ? 1f : 0f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawQuaraDroplet(Vector2 center, Vector2 size, float progress)
        {
            LoadAssets();
            Draw(quaraWaterProjectile, "QuaraDroplet", circle, smoothNoise,
                center, size, 0f, WaterDark, WaterMid, WaterCore,
                0.84f, progress, 1f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawQuaraTidalCrest(Vector2 center, Vector2 drawSize, Texture2D waveTexture, Rectangle sourceFrame, int direction)
        {
            LoadAssets();
            Draw(quaraTidalCrest, "QuaraTidalCrest", waveTexture, wavyDetailNoise.Value,
                center, drawSize, 0f, WaterDark, WaterMid, WaterCore,
                0.92f, 0.5f, 1f, direction, BlendState.AlphaBlend, sourceFrame);
        }

        internal static void DrawQuaraTideRush(Vector2 center, Vector2 drawSize, float progress, bool reforming, int direction)
        {
            LoadAssets();
            // Draw on a circle quad — the shader creates its own procedural puddle shape
            // so we no longer need the NPC sprite texture or source frame
            Draw(quaraTideRush, "QuaraTideRush", circle, wavyDetailNoise,
                center, drawSize * 1.6f, 0f, WaterDark, WaterMid, WaterCore,
                0.92f, progress, reforming ? 1f : 0f, direction, BlendState.AlphaBlend);
        }

        internal static void DrawQuaraInkGeyser(Vector2 center, float progress, bool active)
        {
            LoadAssets();
            Draw(quaraInkGeyser, "QuaraInkGeyser", circle, brokenNoise,
                center, Vector2.One * 80f, 0f, InkDark, InkMid, InkCore,
                active ? 0.9f : 0.72f, progress, active ? 1f : 0f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawQuaraInkJet(Vector2 center, Vector2 velocity)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            LoadAssets();
            Draw(quaraInkGeyser, "QuaraInkJet", windstreak, brokenNoise,
                center - direction * 31f, new Vector2(82f, 26f), direction.ToRotation(),
                InkDark, InkMid, InkCore, 0.84f, 0.5f, 1f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawGreatBlackKnightFlail(Vector2 center, Vector2 motion, bool empowered)
        {
            LoadAssets();
            if (motion.LengthSquared() > 0.25f)
            {
                Vector2 direction = motion.SafeNormalize(Vector2.UnitX);
                Draw(greatBlackKnightFlail, "GreatBlackKnightFlailTrail", veinNoise, cloudNoise,
                    center - direction * 34f, new Vector2(92f, 34f), direction.ToRotation(),
                    GreatPlagueDark, GreatPlagueMid, GreatPlagueCore,
                    empowered ? 0.78f : 0.42f, 0.5f, empowered ? 1f : 0f, 1f, BlendState.AlphaBlend);
            }
            Draw(greatBlackKnightFlail, "GreatBlackKnightFlailHead", perlinTiled, veinNoise,
                center, Vector2.One * (empowered ? 62f : 50f), 0f,
                GreatPlagueDark, GreatPlagueMid, GreatPlagueCore,
                empowered ? 0.9f : 0.54f, 0.5f, empowered ? 1f : 0f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawGreatBlackKnightFlailPulse(Vector2 center, float progress)
        {
            LoadAssets();
            Draw(greatBlackKnightFlail, "GreatBlackKnightFlailPulse", perlinTiled, veinNoise,
                center, Vector2.One * 110f, 0f, GreatPlagueDark, GreatPlagueMid, GreatPlagueCore,
                0.9f, progress, 1f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawGreatBlackKnightEmber(Vector2 center, Vector2 velocity, Vector2 size, float opacity)
        {
            Vector2 direction = velocity.SafeNormalize(-Vector2.UnitY);
            LoadAssets();
            Draw(greatBlackKnightFlail, "GreatBlackKnightFlailEmber", wavyDetailNoise, cloudNoise,
                center - direction * size.X * 0.25f, size, direction.ToRotation(),
                GreatPlagueDark, GreatPlagueMid, GreatPlagueCore, opacity, 0.5f, 1f, 1f,
                BlendState.AlphaBlend);
        }

        /// <summary>
        /// Draws a reusable large void AOE. Its animation is 11.1% larger than the supplied damage
        /// radius, making the collision radius exactly 90% of the full visual radius without drawing
        /// a circle, ring, or other imprint of that mechanical boundary.
        /// </summary>
        internal static void DrawVoidExplosion(Vector2 center, float radius, float progress, float opacity,
            bool active, Color darkColor, Color midColor, Color accentColor, Color coreColor,
            float pixelBlockSize = 2f, float rotation = 0f)
        {
            LoadAssets();
            const float visualScale = 1f / 0.9f;
            const int columns = 5;
            const int rows = 5;
            Texture2D texture = fireExplosion.Value;
            int frameWidth = texture.Width / columns;
            int frameHeight = texture.Height / rows;
            int frameCount = active ? columns * rows : 13;
            int frame = System.Math.Min(frameCount - 1,
                (int)(MathHelper.Clamp(progress, 0f, 1f) * (frameCount - 1)));
            Rectangle source = new(frame % columns * frameWidth, frame / columns * frameHeight,
                frameWidth, frameHeight);

            float clampedProgress = MathHelper.Clamp(progress, 0f, 1f);
            float growth = clampedProgress * (2f - clampedProgress);
            float animationScale = active ? 1f : MathHelper.Lerp(0.1f, 1f, growth);
            Vector2 drawSize = Vector2.One * radius * 2f * visualScale * animationScale;
            Vector2 frameScale = drawSize / new Vector2(frameWidth, frameHeight);
            // Each frame's blast sits off its cell centre; that correction turns with the quad so the blast stays
            // centred on the caller's point at any rotation.
            Vector2 frameCenterCorrection = (VoidExplosionFrameCenterOffsets[frame] * frameScale).RotatedBy(rotation);
            Vector2 adjustedCenter = center - frameCenterCorrection;

            Draw(voidExplosion, active ? "VoidExplosionBlast" : "VoidExplosionCharge",
                texture, voidExplosionDetail.Value, adjustedCenter, drawSize, rotation,
                darkColor, midColor, coreColor, opacity, progress, active ? 1f : 0f,
                1f, BlendState.AlphaBlend, sourceRectangle: source,
                accentColor: accentColor,
                pixelBlockSize: pixelBlockSize);
        }

        internal static void DrawBurst(EnemyVFXBurstKind kind, Vector2 center, float progress, float opacity)
        {
            LoadAssets();
            switch (kind)
            {
                case EnemyVFXBurstKind.BlackKnightHexShatter:
                    Draw(blackKnightHexCrystal, "BlackKnightHexShatter", perlinTiled, veinNoise,
                        center, Vector2.One * 82f, 0f, HexGoldDark, HexGoldMid, HexGoldCore, opacity, progress, 1f, 1f);
                    break;
                case EnemyVFXBurstKind.BlackKnightMoonfuryBlast:
                case EnemyVFXBurstKind.GreatBlackKnightBlightBombBlast:
                    bool blightBomb = kind == EnemyVFXBurstKind.GreatBlackKnightBlightBombBlast;
                    Draw(blackKnightMoonfury, "BlackKnightMoonfurySmoke", perlinTiled, cloudNoise,
                        center, Vector2.One * 170f, 0f, blightBomb ? BlightDark : PlagueDark,
                        blightBomb ? BlightMid : PlagueMid, blightBomb ? BlightCore : PlagueCore,
                        opacity * 0.45f, progress, 1f, 1f, BlendState.AlphaBlend);
                    Draw(blackKnightMoonfury, "BlackKnightMoonfuryBlast", cloudNoise, turbulentNoise,
                        center, Vector2.One * 109f, 0f, blightBomb ? BlightDark : PlagueDark,
                        blightBomb ? BlightMid : PlagueMid, blightBomb ? BlightCore : PlagueCore,
                        opacity, progress, 1f, 1f);
                    break;
                case EnemyVFXBurstKind.BlackKnightSpearImpact:
                    Draw(blackKnightSpearWake, "BlackKnightSpearImpact", perlinTiled, cloudNoise,
                        center, Vector2.One * 84f, 0f, PlagueAshDark, PlagueAshMid, PlagueAshCore, opacity, progress, 1f, 1f);
                    break;
                case EnemyVFXBurstKind.DemonSpiritSoulBurst:
                    Draw(demonSpiritSoulComet, "DemonSpiritSoulBurst", circle, brokenNoise,
                        center, Vector2.One * 204f, 0f, CurseDark, CurseMid, CurseCore, opacity, progress, 1f, 1f);
                    break;
                case EnemyVFXBurstKind.EvilEyeFlameImpact:
                    Draw(evilEyeFlameTrail, "EvilEyeFlameImpact", circle, smoothNoise,
                        center, Vector2.One * 48f, 0f, EyeDark, EyeMid, EyeCore, opacity, progress, 1f, 1f);
                    break;
                case EnemyVFXBurstKind.ElandVenomImpact:
                    Draw(elandToxicVFX, "ElandVenomImpact", voronoiNoise, cloudNoise,
                        center, Vector2.One * 92f, 0f, ToxicDark, ToxicMid, ToxicCore,
                        opacity, progress, 1f, 1f, BlendState.AlphaBlend);
                    break;
                case EnemyVFXBurstKind.QuaraWaterBurst:
                    Draw(quaraWaterProjectile, "QuaraWaterBurst", circle, wavyDetailNoise,
                        center, Vector2.One * 132f, 0f, WaterDark, WaterMid, WaterCore,
                        opacity, progress, 1f, 1f, BlendState.AlphaBlend);
                    break;
                case EnemyVFXBurstKind.QuaraInkBurst:
                    Draw(quaraInkGeyser, "QuaraInkBurst", circle, brokenNoise,
                        center, Vector2.One * 92f, 0f, InkDark, InkMid, InkCore,
                        opacity, progress, 1f, 1f, BlendState.AlphaBlend);
                    break;
                case EnemyVFXBurstKind.EvilEyeGhostBurst:
                    Draw(evilEyeDeathBurst, "EvilEyeGhostBurst", circle, brokenNoise,
                        center, Vector2.One * 200f, 0f, EyeDark, EyeMid, EyeCore,
                        opacity, progress, 1f, 1f, BlendState.AlphaBlend);
                    break;
                case EnemyVFXBurstKind.EvilEyeTeleportBurst:
                    // Own technique now, not DemonSpiritSoulBurst: that one bakes in a square SDF
                    // that drew a white rectangle around the portal, and it is shared with
                    // DemonSpirit's live explosion so it can't be edited in place.
                    Draw(evilEyeTeleport, "EvilEyeTeleportRift", perlinTiled, swirlyNoise,
                        center, Vector2.One * 300f, 0f, CurseDark, CurseMid, CurseCore, opacity, progress, 1f, 1f);
                    break;
                case EnemyVFXBurstKind.MantassinGlintFlash:
                    // Nail shatter / bounce spark: the nail glint blown up and fading out as the burst runs.
                    float flashFade = 1f - progress;
                    Draw(mantassinGlintstone, "MantassinNailGlint", cloudNoise, brokenNoise,
                        center, Vector2.One * MathHelper.Lerp(40f, 76f, progress), 0f, Color.Black, MantassinMid, MantassinCore,
                        opacity * flashFade, flashFade, 1f, 1f, BlendState.AlphaBlend);
                    break;
                case EnemyVFXBurstKind.MantassinBlink:
                    // The EvilEye space-distorting rift, recoloured to the Mantassin's silver glint.
                    Draw(evilEyeTeleport, "EvilEyeTeleportRift", perlinTiled, swirlyNoise,
                        center, Vector2.One * 180f, 0f, MantassinBlinkDark, MantassinMid, MantassinCore, opacity, progress, 1f, 1f);
                    break;
            }
        }

        static void Draw(Asset<Effect> effectAsset, string techniqueName,
            Asset<Texture2D> primaryAsset, Asset<Texture2D> detailAsset,
            Vector2 worldCenter, Vector2 drawSize, float rotation,
            Color darkColor, Color midColor, Color coreColor,
            float opacity, float progress, float active, float direction,
            BlendState blendState = null, Color? accentColor = null, float pixelBlockSize = 2f)
        {
            if (primaryAsset == null)
            {
                return;
            }
            Draw(effectAsset, techniqueName, primaryAsset.Value, detailAsset?.Value,
                worldCenter, drawSize, rotation, darkColor, midColor, coreColor,
                opacity, progress, active, direction, blendState, null, accentColor, pixelBlockSize);
        }

        static void Draw(Asset<Effect> effectAsset, string techniqueName,
            Texture2D primaryTexture, Texture2D detailTexture,
            Vector2 worldCenter, Vector2 drawSize, float rotation,
            Color darkColor, Color midColor, Color coreColor,
            float opacity, float progress, float active, float direction,
            BlendState blendState = null, Rectangle? sourceRectangle = null,
            Color? accentColor = null, float pixelBlockSize = 2f)
        {
            if (Main.dedServ || effectAsset == null || primaryTexture == null)
            {
                return;
            }

            blendState ??= BlendState.Additive;
            Vector2 actualSize = sourceRectangle.HasValue ? sourceRectangle.Value.Size() : primaryTexture.Size();
            Vector2 scale = drawSize / actualSize;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            Effect effect = effectAsset.Value;

            try
            {
                if (detailTexture != null)
                {
                    graphicsDevice.Textures[1] = detailTexture;
                    graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                }
                effect.CurrentTechnique = effect.Techniques[techniqueName];
                Vector4 uSourceRect = sourceRectangle.HasValue
                    ? new Vector4(
                        sourceRectangle.Value.X / (float)primaryTexture.Width,
                        sourceRectangle.Value.Y / (float)primaryTexture.Height,
                        sourceRectangle.Value.Width / (float)primaryTexture.Width,
                        sourceRectangle.Value.Height / (float)primaryTexture.Height)
                    : new Vector4(0f, 0f, 1f, 1f);

                effect.Parameters["DarkColor"]?.SetValue(darkColor.ToVector3());
                effect.Parameters["MidColor"]?.SetValue(midColor.ToVector3());
                effect.Parameters["AccentColor"]?.SetValue((accentColor ?? midColor).ToVector3());
                effect.Parameters["CoreColor"]?.SetValue(coreColor.ToVector3());
                effect.Parameters["Opacity"]?.SetValue(opacity);
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"]?.SetValue(progress);
                effect.Parameters["Active"]?.SetValue(active);
                effect.Parameters["Direction"]?.SetValue(direction);
                effect.Parameters["DrawSize"]?.SetValue(actualSize);
                effect.Parameters["PixelDrawSize"]?.SetValue(drawSize);
                // Pre-divided pixel grid: xy = block count across the quad, zw = its reciprocal.
                // Deliberately computed HERE rather than in HLSL. A raw ps_2_0 entry point has no
                // preshader, so uniform-only math (the max() and the reciprocal) is emitted per pixel
                // — measured at 12 arithmetic slots for BlackKnightHexCrystal's seal techniques, which
                // is the difference between fitting the 64-slot Reach budget and not. Any technique
                // that wants pixelation should take this rather than re-deriving it from a size.
                Vector2 pixelBlocks = Vector2.Max(drawSize / pixelBlockSize, Vector2.One);
                effect.Parameters["PixelGrid"]?.SetValue(
                    new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y));
                effect.Parameters["PrimaryTextureSize"]?.SetValue(primaryTexture.Size());
                effect.Parameters["uSourceRect"]?.SetValue(uSourceRect);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primaryTexture, worldCenter - Main.screenPosition, sourceRectangle, Color.White,
                    rotation, actualSize * 0.5f, scale, SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }
    }

    internal class EnemyShaderBurst : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        EnemyVFXBurstKind Kind => (EnemyVFXBurstKind)(int)Projectile.ai[0];
        int Duration => Kind switch
        {
            // Was 20t (~0.33s) — "like only a frame before it disappears" per playtest. +30t so the
            // fireball/cool-down curve in BlackKnightMoonfury.fx's Blast technique is actually visible.
            EnemyVFXBurstKind.BlackKnightMoonfuryBlast or EnemyVFXBurstKind.GreatBlackKnightBlightBombBlast => 50,
            EnemyVFXBurstKind.DemonSpiritSoulBurst => 18,
            // Hold long enough for the gravity-driven impact spray to visibly arc and fall.
            EnemyVFXBurstKind.QuaraWaterBurst => 48,
            EnemyVFXBurstKind.EvilEyeGhostBurst => 34,
            // Combo B's dark poison splats need time to merge into a readable cloud after the
            // balls land. Damage is handled separately by ElandVenomSplash (14 ticks only), so
            // this longer lifetime is purely a harmless visual residue.
            EnemyVFXBurstKind.ElandVenomImpact => 135,
            // User's explicit ask: long enough to actually see it (~60t), independent of
            // DemonSpiritSoulBurst's own 18t so DemonSpirit's real explosion is untouched.
            EnemyVFXBurstKind.EvilEyeTeleportBurst => 60,
            EnemyVFXBurstKind.MantassinGlintFlash => 12,
            EnemyVFXBurstKind.MantassinBlink => 40,
            _ => 14
        };

        int FadeDuration => Kind switch
        {
            // Let Eland's impact cloud diffuse away over 1.25 seconds instead of dropping out in
            // the generic five-tick burst fade. This makes successive Combo B impacts overlap as
            // lingering poison smog. The fade starts as Combo B's paired 60-tick damage window ends.
            EnemyVFXBurstKind.ElandVenomImpact => 75,
            EnemyVFXBurstKind.QuaraWaterBurst => 18,
            _ => 5
        };

        public static void Spawn(Terraria.DataStructures.IEntitySource source, Vector2 center, EnemyVFXBurstKind kind)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(source, center, Vector2.Zero, ModContent.ProjectileType<EnemyShaderBurst>(),
                    0, 0f, Main.myPlayer, (float)kind);
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 20;
            Projectile.penetrate = -1;
            Projectile.hostile = false;
            Projectile.friendly = false;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Duration;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Projectile.localAI[0] / Duration, 0f, 1f);
            float fade = MathHelper.SmoothStep(0f, 1f,
                MathHelper.Clamp(Projectile.timeLeft / (float)FadeDuration, 0f, 1f));
            EnemyVFX.DrawBurst(Kind, Projectile.Center, progress, fade);
            return false;
        }
    }

    internal class BlackKnightGravefallTelegraph : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 4;
            Projectile.hostile = false;
            Projectile.friendly = false;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EnemyVFX.DrawBlackKnightGraveTear(Projectile.Center,
                MathHelper.Clamp(Projectile.localAI[0] / 3f, 0f, 1f));
            return false;
        }
    }
}

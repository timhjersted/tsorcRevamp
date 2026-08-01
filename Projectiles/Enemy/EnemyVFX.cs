using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
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
        QuaraInkBurst
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
        static Asset<Effect> elandToxicVFX;
        static Asset<Effect> quaraWaterVFX;
        static Asset<Effect> quaraInkVFX;
        static Asset<Effect> greatBlackKnightFlail;

        static Asset<Texture2D> circle;
        static Asset<Texture2D> gradient;
        static Asset<Texture2D> spiral;
        static Asset<Texture2D> windstreak;
        static Asset<Texture2D> trail;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> smoke;
        static Asset<Texture2D> aura;
        static Asset<Texture2D> flare;

        static readonly Color CurseDark = new(8, 4, 15);
        static readonly Color CurseMid = new(98, 38, 138);
        static readonly Color CurseCore = new(236, 216, 255);
        static readonly Color SteelDark = new(19, 17, 22);
        static readonly Color SteelMid = new(139, 47, 52);
        static readonly Color SteelCore = new(226, 232, 238);
        static readonly Color EyeDark = new(8, 12, 34);
        static readonly Color EyeMid = new(54, 112, 226);
        static readonly Color EyeCore = new(214, 246, 255);
        static readonly Color ToxicDark = new(8, 25, 8);
        static readonly Color ToxicMid = new(66, 176, 54);
        static readonly Color ToxicCore = new(213, 255, 133);
        static readonly Color WaterDark = new(5, 25, 52);
        static readonly Color WaterMid = new(32, 150, 221);
        static readonly Color WaterCore = new(213, 249, 255);
        static readonly Color InkDark = new(3, 3, 9);
        static readonly Color InkMid = new(30, 25, 67);
        static readonly Color InkCore = new(142, 197, 226);
        static readonly Color FrostCurseDark = new(4, 7, 18);
        static readonly Color FrostCurseMid = new(42, 78, 142);
        static readonly Color FrostCurseCore = new(204, 241, 255);

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
            elandToxicVFX ??= ModContent.Request<Effect>(EffectRoot + "ElandToxicVFX", AssetRequestMode.ImmediateLoad);
            quaraWaterVFX ??= ModContent.Request<Effect>(EffectRoot + "QuaraWaterVFX", AssetRequestMode.ImmediateLoad);
            quaraInkVFX ??= ModContent.Request<Effect>(EffectRoot + "QuaraInkVFX", AssetRequestMode.ImmediateLoad);
            greatBlackKnightFlail ??= ModContent.Request<Effect>(EffectRoot + "GreatBlackKnightFlail", AssetRequestMode.ImmediateLoad);

            circle ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_CircleFit1", AssetRequestMode.ImmediateLoad);
            gradient ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Gradient_circle22", AssetRequestMode.ImmediateLoad);
            spiral ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Spiral07", AssetRequestMode.ImmediateLoad);
            windstreak ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Windstreak3", AssetRequestMode.ImmediateLoad);
            trail ??= ModContent.Request<Texture2D>(NoiseRoot + "T_trail12", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            smoke ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_RoundSmoke71", AssetRequestMode.ImmediateLoad);
            aura ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            flare ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawBlackKnightHexCrystal(Vector2 center, Vector2 velocity, float dormantProgress, bool active)
        {
            LoadAssets();
            if (active)
            {
                Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
                Draw(blackKnightHexCrystal, "BlackKnightHexComet", windstreak, brokenNoise,
                    center - direction * 36f, new Vector2(96f, 30f), direction.ToRotation(),
                    CurseDark, CurseMid, CurseCore, 0.82f, dormantProgress, 1f, 1f);
            }
            Draw(blackKnightHexCrystal, "BlackKnightHexSeal", circle, brokenNoise,
                center, active ? new Vector2(44f, 36f) : Vector2.One * 74f, 0f,
                CurseDark, CurseMid, CurseCore, active ? 0.9f : 0.74f,
                dormantProgress, active ? 1f : 0f, 1f);
        }

        internal static void DrawBlackKnightDeathSeal(Vector2 center, float progress)
        {
            LoadAssets();
            float size = MathHelper.Lerp(500f, 96f, progress);
            Draw(blackKnightDeathVolley, "BlackKnightDeathSeal", circle, brokenNoise,
                center, Vector2.One * size, 0f, CurseDark, CurseMid, CurseCore,
                0.78f, progress, 1f, 1f);
        }

        internal static void DrawBlackKnightAimThread(Vector2 start, Vector2 end, float progress)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 2f)
            {
                return;
            }
            LoadAssets();
            Draw(blackKnightDeathVolley, "BlackKnightAimThread", trail, smoothNoise,
                Vector2.Lerp(start, end, 0.5f), new Vector2(length, 12f), delta.ToRotation(),
                CurseDark, CurseMid, CurseCore, 0.48f, progress, 1f, 1f);
        }

        internal static void DrawBlackKnightDeathTrail(Vector2 center, Vector2 velocity, Vector2 size, float opacity)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            LoadAssets();
            Draw(blackKnightDeathVolley, "BlackKnightDeathTrail", windstreak, brokenNoise,
                center - direction * size.X * 0.28f, size, direction.ToRotation(),
                CurseDark, CurseMid, CurseCore, opacity, 0.5f, 1f, 1f);
        }

        internal static void DrawBlackKnightGraveTear(Vector2 center, float progress)
        {
            LoadAssets();
            Draw(blackKnightGravefall, "BlackKnightGraveTear", circle, brokenNoise,
                center, new Vector2(104f, 42f), 0f, CurseDark, CurseMid, CurseCore,
                0.88f, progress, 0f, 1f);
        }

        internal static void DrawBlackKnightGraveTrail(Vector2 center, Vector2 velocity)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitY);
            LoadAssets();
            Draw(blackKnightGravefall, "BlackKnightGraveTrail", windstreak, smoothNoise,
                center - direction * 32f, new Vector2(86f, 22f), direction.ToRotation(),
                CurseDark, CurseMid, CurseCore, 0.68f, 0.5f, 1f, 1f);
        }

        internal static void DrawBlackKnightMoonfury(Vector2 center, Vector2 velocity, float progress, bool active)
        {
            LoadAssets();
            if (velocity.LengthSquared() > 0.2f)
            {
                Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
                Draw(blackKnightMoonfury, "BlackKnightMoonfurySmoke", smoke, brokenNoise,
                    center - direction * 31f, new Vector2(82f, 34f), direction.ToRotation(),
                    CurseDark, CurseMid, CurseCore, 0.46f, progress, active ? 1f : 0f, 1f, BlendState.AlphaBlend);
            }
            Draw(blackKnightMoonfury, "BlackKnightMoonfuryCoal", gradient, brokenNoise,
                center, Vector2.One * (active ? 70f : 48f), 0f,
                CurseDark, CurseMid, CurseCore, 0.88f, progress, active ? 1f : 0f, 1f);
        }

        internal static void DrawBlackKnightSpearWake(Vector2 center, float rotation, Vector2 size, float opacity)
        {
            LoadAssets();
            Draw(blackKnightSpearWake, "BlackKnightSpearWake", windstreak, brokenNoise,
                center, size, rotation, SteelDark, SteelMid, SteelCore,
                opacity, 0.5f, 1f, 1f);
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
                int rayCount = pattern == 0 ? 1 : pattern == 1 ? 3 : 2;
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
            Draw(demonSpiritSoulComet, "DemonSpiritSoulComet", aura, brokenNoise,
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
            Draw(evilEyeFlameTrail, "EvilEyeFlameTail", windstreak, smoothNoise,
                center - direction * 25f, new Vector2(68f, 22f), direction.ToRotation(),
                EyeDark, EyeMid, EyeCore, 0.8f, 0.5f, seeking ? 1f : 0f, 1f);
            Draw(evilEyeFlameTrail, "EvilEyeFlameCore", gradient, smoothNoise,
                center, Vector2.One * 18f, 0f, EyeDark, EyeMid, EyeCore,
                0.95f, 0.5f, seeking ? 1f : 0f, 1f);
        }

        internal static void DrawElandToxicField(Vector2 center, Vector2 size, float progress, bool active, bool circular)
        {
            LoadAssets();
            Draw(elandToxicVFX, "ElandToxicField", circle, brokenNoise,
                center, size, 0f, ToxicDark, ToxicMid, ToxicCore,
                active ? 0.78f : 0.68f, progress, active ? 1f : 0f, circular ? 1f : 0f,
                BlendState.AlphaBlend);
        }

        internal static void DrawElandVenomProjectile(Vector2 center, Vector2 velocity, Vector2 size, float opacity = 0.8f)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            LoadAssets();
            Draw(elandToxicVFX, "ElandVenomGlob", windstreak, brokenNoise,
                center - direction * size.X * 0.28f, size, direction.ToRotation(),
                ToxicDark, ToxicMid, ToxicCore, opacity, 0.5f, 1f, 1f);
        }

        internal static void DrawQuaraCast(Vector2 center, float progress, int pattern)
        {
            LoadAssets();
            Color mid = pattern == 3 ? new Color(28, 42, 78) : WaterMid;
            Color core = pattern == 3 ? InkCore : WaterCore;
            Draw(quaraWaterVFX, "QuaraWaterCast", circle, smoothNoise,
                center, Vector2.One * (58f + pattern * 5f), pattern * 0.22f,
                WaterDark, mid, core, 0.76f, progress, pattern, 1f);
        }

        internal static void DrawQuaraBubble(Vector2 center, Vector2 size, float progress, bool pressurized)
        {
            LoadAssets();
            Draw(quaraWaterVFX, "QuaraBubble", circle, smoothNoise,
                center, size, 0f, WaterDark, WaterMid, WaterCore,
                0.88f, progress, pressurized ? 1f : 0f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawQuaraTidalCrest(Vector2 center, Vector2 size, int direction)
        {
            LoadAssets();
            Draw(quaraWaterVFX, "QuaraTidalCrest", gradient, smoothNoise,
                center, size, 0f, WaterDark, WaterMid, WaterCore,
                0.82f, 0.5f, 1f, direction, BlendState.AlphaBlend);
        }

        internal static void DrawQuaraTideRush(Vector2 center, Vector2 size, float progress, bool reforming, int direction)
        {
            LoadAssets();
            Draw(quaraWaterVFX, "QuaraTideRush", gradient, smoothNoise,
                center, size, 0f, WaterDark, WaterMid, WaterCore,
                0.84f, progress, reforming ? 1f : 0f, direction, BlendState.AlphaBlend);
            if (!reforming)
            {
                Draw(quaraWaterVFX, "QuaraTidalCrest", windstreak, smoothNoise,
                    center - new Vector2(direction * 20f, 0f), new Vector2(64f, size.Y), 0f,
                    WaterDark, WaterMid, WaterCore, 0.34f, progress, 1f, direction, BlendState.AlphaBlend);
            }
        }

        internal static void DrawQuaraInkGeyser(Vector2 center, float progress, bool active)
        {
            LoadAssets();
            Draw(quaraInkVFX, "QuaraInkGeyser", circle, brokenNoise,
                center, Vector2.One * 80f, 0f, InkDark, InkMid, InkCore,
                active ? 0.9f : 0.72f, progress, active ? 1f : 0f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawQuaraInkJet(Vector2 center, Vector2 velocity)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            LoadAssets();
            Draw(quaraInkVFX, "QuaraInkJet", windstreak, brokenNoise,
                center - direction * 31f, new Vector2(82f, 26f), direction.ToRotation(),
                InkDark, InkMid, InkCore, 0.84f, 0.5f, 1f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawGreatBlackKnightFlail(Vector2 center, Vector2 motion, bool empowered)
        {
            LoadAssets();
            if (motion.LengthSquared() > 0.25f)
            {
                Vector2 direction = motion.SafeNormalize(Vector2.UnitX);
                Draw(greatBlackKnightFlail, "GreatBlackKnightFlailTrail", windstreak, brokenNoise,
                    center - direction * 34f, new Vector2(92f, 34f), direction.ToRotation(),
                    FrostCurseDark, FrostCurseMid, FrostCurseCore,
                    empowered ? 0.78f : 0.42f, 0.5f, empowered ? 1f : 0f, 1f, BlendState.AlphaBlend);
            }
            Draw(greatBlackKnightFlail, "GreatBlackKnightFlailHead", gradient, brokenNoise,
                center, Vector2.One * (empowered ? 62f : 50f), 0f,
                FrostCurseDark, FrostCurseMid, FrostCurseCore,
                empowered ? 0.9f : 0.54f, 0.5f, empowered ? 1f : 0f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawGreatBlackKnightFlailPulse(Vector2 center, float progress)
        {
            LoadAssets();
            Draw(greatBlackKnightFlail, "GreatBlackKnightFlailPulse", circle, brokenNoise,
                center, Vector2.One * 110f, 0f, FrostCurseDark, FrostCurseMid, FrostCurseCore,
                0.9f, progress, 1f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawGreatBlackKnightEmber(Vector2 center, Vector2 velocity, Vector2 size, float opacity)
        {
            Vector2 direction = velocity.SafeNormalize(-Vector2.UnitY);
            LoadAssets();
            Draw(greatBlackKnightFlail, "GreatBlackKnightFlailEmber", windstreak, smoothNoise,
                center - direction * size.X * 0.25f, size, direction.ToRotation(),
                FrostCurseDark, FrostCurseMid, FrostCurseCore, opacity, 0.5f, 1f, 1f,
                BlendState.AlphaBlend);
        }

        internal static void DrawBurst(EnemyVFXBurstKind kind, Vector2 center, float progress, float opacity)
        {
            LoadAssets();
            switch (kind)
            {
                case EnemyVFXBurstKind.BlackKnightHexShatter:
                    Draw(blackKnightHexCrystal, "BlackKnightHexShatter", flare, brokenNoise,
                        center, Vector2.One * 82f, 0f, CurseDark, CurseMid, CurseCore, opacity, progress, 1f, 1f);
                    break;
                case EnemyVFXBurstKind.BlackKnightMoonfuryBlast:
                    Draw(blackKnightMoonfury, "BlackKnightMoonfurySmoke", smoke, brokenNoise,
                        center, Vector2.One * 170f, 0f, CurseDark, CurseMid, CurseCore,
                        opacity * 0.45f, progress, 1f, 1f, BlendState.AlphaBlend);
                    Draw(blackKnightMoonfury, "BlackKnightMoonfuryBlast", circle, brokenNoise,
                        center, Vector2.One * 109f, 0f, CurseDark, CurseMid, CurseCore, opacity, progress, 1f, 1f);
                    break;
                case EnemyVFXBurstKind.BlackKnightSpearImpact:
                    Draw(blackKnightSpearWake, "BlackKnightSpearImpact", circle, brokenNoise,
                        center, Vector2.One * 84f, 0f, SteelDark, SteelMid, SteelCore, opacity, progress, 1f, 1f);
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
                    Draw(elandToxicVFX, "ElandVenomImpact", circle, brokenNoise,
                        center, Vector2.One * 92f, 0f, ToxicDark, ToxicMid, ToxicCore,
                        opacity, progress, 1f, 1f, BlendState.AlphaBlend);
                    break;
                case EnemyVFXBurstKind.QuaraWaterBurst:
                    Draw(quaraWaterVFX, "QuaraWaterBurst", circle, smoothNoise,
                        center, Vector2.One * 104f, 0f, WaterDark, WaterMid, WaterCore,
                        opacity, progress, 1f, 1f, BlendState.AlphaBlend);
                    break;
                case EnemyVFXBurstKind.QuaraInkBurst:
                    Draw(quaraInkVFX, "QuaraInkBurst", circle, brokenNoise,
                        center, Vector2.One * 92f, 0f, InkDark, InkMid, InkCore,
                        opacity, progress, 1f, 1f, BlendState.AlphaBlend);
                    break;
            }
        }

        static void Draw(Asset<Effect> effectAsset, string techniqueName,
            Asset<Texture2D> primaryAsset, Asset<Texture2D> detailAsset,
            Vector2 worldCenter, Vector2 drawSize, float rotation,
            Color darkColor, Color midColor, Color coreColor,
            float opacity, float progress, float active, float direction,
            BlendState blendState = null)
        {
            if (Main.dedServ || effectAsset == null || primaryAsset == null || detailAsset == null)
            {
                return;
            }

            blendState ??= BlendState.Additive;
            Texture2D primaryTexture = primaryAsset.Value;
            Texture2D detailTexture = detailAsset.Value;
            int sourceWidth = System.Math.Clamp((int)drawSize.X, 1, primaryTexture.Width);
            int sourceHeight = System.Math.Clamp((int)drawSize.Y, 1, primaryTexture.Height);
            Rectangle source = new(0, 0, sourceWidth, sourceHeight);
            Vector2 actualSize = source.Size();
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
                graphicsDevice.Textures[1] = detailTexture;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                effect.CurrentTechnique = effect.Techniques[techniqueName];
                effect.Parameters["DarkColor"]?.SetValue(darkColor.ToVector3());
                effect.Parameters["MidColor"]?.SetValue(midColor.ToVector3());
                effect.Parameters["CoreColor"]?.SetValue(coreColor.ToVector3());
                effect.Parameters["Opacity"]?.SetValue(opacity);
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"]?.SetValue(progress);
                effect.Parameters["Active"]?.SetValue(active);
                effect.Parameters["Direction"]?.SetValue(direction);
                effect.Parameters["DrawSize"]?.SetValue(actualSize);
                effect.Parameters["PrimaryTextureSize"]?.SetValue(primaryTexture.Size());
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primaryTexture, worldCenter - Main.screenPosition, source, Color.White,
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
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        EnemyVFXBurstKind Kind => (EnemyVFXBurstKind)(int)Projectile.ai[0];
        int Duration => Kind switch
        {
            EnemyVFXBurstKind.BlackKnightMoonfuryBlast => 20,
            EnemyVFXBurstKind.DemonSpiritSoulBurst => 18,
            EnemyVFXBurstKind.QuaraWaterBurst => 18,
            _ => 14
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
            float fade = MathHelper.Clamp(Projectile.timeLeft / 5f, 0f, 1f);
            EnemyVFX.DrawBurst(Kind, Projectile.Center, progress, fade);
            return false;
        }
    }

    internal class BlackKnightGravefallTelegraph : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

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

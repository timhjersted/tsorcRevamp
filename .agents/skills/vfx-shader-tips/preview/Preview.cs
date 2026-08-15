// Offline CPU preview harness for Terraria .fx pixel shaders.
//
// WHY: you cannot see an .fx file. Compiling it only proves it fits in ps_2_0. Every visual bug in
// the Nito pass — additive colour clipping to white over a bright sky, a "cracked stone" texture
// reading as circuitry, fire shaped like popcorn, three rounds of over-bright colour weights — was
// invisible in the HLSL and obvious the moment it was rendered. This costs about ten minutes to wire
// up per boss and saves entire playtest cycles.
//
// HOW: port each pixel-shader function to C# almost verbatim (the helpers below are named to make
// that a near-copy-paste), sample the real PNGs the way the GPU will, composite with the same blend
// equation Terraria uses, and save a contact sheet.
//
// THIS IS NOT COMPILED INTO THE MOD. It lives under .agents/, which MSBuild's default globs skip
// (verified with a deliberately-broken probe file). Run it standalone:
//     cd .agents/skills/vfx-shader-tips/preview && dotnet run
//
// It is a TEMPLATE. Replace the example techniques in the "YOUR SHADERS" region and the panel table.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

static class Preview
{
    // ---------------------------------------------------------------------------------------------
    // Harness. You should not need to touch anything above "YOUR SHADERS".
    // ---------------------------------------------------------------------------------------------

    // An explicit root lets a staged preview build run outside the repository while continuing to
    // sample the exact game textures. The default preserves the normal in-place workflow.
    static string NoiseRoot => Environment.GetEnvironmentVariable("PREVIEW_NOISE_ROOT") ?? @"..\..\..\..\Textures\Noise\";
    static string TextureRoot => Environment.GetEnvironmentVariable("PREVIEW_TEXTURE_ROOT") ?? @"..\..\..\..\Textures\";

    /// A texture sampled the way the GPU will sample it.
    ///
    /// Two details that matter and are easy to get wrong:
    ///  1. tModLoader PREMULTIPLIES textures on load, so a shader's `tex2D(s, uv).r` is really
    ///     `R_raw * A`. A texture whose alpha channel carries the image (T_Windstreak3) therefore
    ///     reads as a constant through .r — reproduce that or the preview lies to you.
    ///  2. The draws use SamplerState.LinearWrap: BILINEAR, and WRAPPING. Nearest sampling makes
    ///     silhouettes look blocky and will send you chasing an artifact that does not exist.
    sealed class Tex
    {
        readonly float[] r, g, b, a;
        readonly int w, h;

        public Tex(string name, bool previewLocal = false)
        {
            using var bmp = new Bitmap((previewLocal ? "" : NoiseRoot) + name + ".png");
            w = bmp.Width; h = bmp.Height;
            r = new float[w * h]; g = new float[w * h]; b = new float[w * h]; a = new float[w * h];
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* p = (byte*)data.Scan0;
                for (int y = 0; y < h; y++)
                {
                    byte* row = p + y * data.Stride;
                    for (int x = 0; x < w; x++)
                    {
                        float av = row[x * 4 + 3] / 255f;
                        int i = y * w + x;
                        b[i] = row[x * 4 + 0] / 255f * av;   // premultiplied, as tModLoader stores them
                        g[i] = row[x * 4 + 1] / 255f * av;
                        r[i] = row[x * 4 + 2] / 255f * av;
                        a[i] = av;
                    }
                }
            }
            bmp.UnlockBits(data);
        }

        /// Full bilinear-wrap sample. `.x/.y/.z` are premultiplied RGB, `.w` is raw alpha —
        /// i.e. exactly what `tex2D()` hands your HLSL.
        public V4 T(float u, float v)
        {
            u -= MathF.Floor(u); v -= MathF.Floor(v);
            float fx = u * w - 0.5f, fy = v * h - 0.5f;
            int x0 = (int)MathF.Floor(fx), y0 = (int)MathF.Floor(fy);
            float tx = fx - x0, ty = fy - y0;
            int x1 = Wrap(x0 + 1, w), y1 = Wrap(y0 + 1, h);
            x0 = Wrap(x0, w); y0 = Wrap(y0, h);
            int i00 = y0 * w + x0, i10 = y0 * w + x1, i01 = y1 * w + x0, i11 = y1 * w + x1;
            return new V4(Bi(r, i00, i10, i01, i11, tx, ty), Bi(g, i00, i10, i01, i11, tx, ty),
                          Bi(b, i00, i10, i01, i11, tx, ty), Bi(a, i00, i10, i01, i11, tx, ty));
        }

        /// Convenience for the overwhelmingly common `tex2D(sampler, uv).r`.
        public float R(float u, float v) => T(u, v).x;
        public float R(V2 uv) => T(uv.x, uv.y).x;
        public V4 T(V2 uv) => T(uv.x, uv.y);

        static int Wrap(int i, int n) => ((i % n) + n) % n;
        static float Bi(float[] s, int i00, int i10, int i01, int i11, float tx, float ty)
            => (s[i00] * (1 - tx) + s[i10] * tx) * (1 - ty) + (s[i01] * (1 - tx) + s[i11] * tx) * ty;
    }

    // HLSL-alike scalar/vector helpers so a ported pixel function reads like the original.
    public struct V2
    {
        public float x, y;
        public V2(float a, float b) { x = a; y = b; }
        public static V2 operator +(V2 a, V2 b) => new(a.x + b.x, a.y + b.y);
        public static V2 operator +(V2 a, float s) => new(a.x + s, a.y + s);
        public static V2 operator -(V2 a, float s) => new(a.x - s, a.y - s);
        public static V2 operator -(V2 a, V2 b) => new(a.x - b.x, a.y - b.y);
        public static V2 operator *(V2 a, float s) => new(a.x * s, a.y * s);
        public static V2 operator *(V2 a, V2 b) => new(a.x * b.x, a.y * b.y);
        public static V2 operator /(V2 a, float s) => new(a.x / s, a.y / s);
    }
    public struct V3
    {
        public float x, y, z;
        public V3(float a, float b, float c) { x = a; y = b; z = c; }
        public static V3 operator +(V3 a, V3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static V3 operator *(V3 a, float s) => new(a.x * s, a.y * s, a.z * s);
    }
    public struct V4
    {
        public float x, y, z, w;
        public V4(float a, float b, float c, float d) { x = a; y = b; z = c; w = d; }
        public float r => x; public float g => y; public float b => z; public float a => w;
    }

    static float sat(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    static float length(V2 p) => MathF.Sqrt(p.x * p.x + p.y * p.y);
    static float abs(float v) => MathF.Abs(v);
    static V3 lerp(V3 a, V3 b, float t) => new(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
    static V2 PixelateShaderUV(V2 uv, int width, int height, float pixelBlockSize = 2f)
    {
        float xBlock = pixelBlockSize / width;
        float yBlock = pixelBlockSize / height;
        return new V2((MathF.Floor(uv.x / xBlock) + .5f) * xBlock, (MathF.Floor(uv.y / yBlock) + .5f) * yBlock);
    }
    /// Author colours the way the C# VFX helper does, so you can paste the palette straight across.
    static V3 C(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f);

    enum Blend { PremultipliedAlpha, Additive }

    /// One technique to render. `w`/`h` are the REAL draw size the call site passes — aspect matters,
    /// several bugs only show up at the shipping dimensions.
    record Panel(string Name, int W, int H, Blend Mode, Func<V2, (V3 rgb, float a)> Shade);

    // ---------------------------------------------------------------------------------------------
    // YOUR SHADERS — port each pixel function here, one per method, then list them in Panels().
    // Keep the HLSL and this copy in sync while you iterate; they diverge fast otherwise.
    // ---------------------------------------------------------------------------------------------

    // ---- Effects/RedKnightDestinedDeath.fx --------------------------------------------------
    // Sampler bindings must match RedKnightVFX.DrawDestinedDeathQuad exactly.
    static Tex MacroSampler, DetailSampler, SolarMacroSampler, SolarDetailSampler, SolarFlameSampler, StoneSampler, CrackSampler, MonolithSampler;
    static Tex ElandPrimarySampler, ElandDetailSampler;

    static void LoadTextures()
    {
        MacroSampler = new Tex("T_Noise_6Yu1");
        DetailSampler = new Tex("Turbulence_07-512x512");
        SolarMacroSampler = new Tex("SmoothNoise");
        SolarDetailSampler = new Tex("Turbulence_06-512x512");
        SolarFlameSampler = new Tex("T_FirePanningCyl45");
        StoneSampler = new Tex("T_Noise_Wo14");
        CrackSampler = new Tex("Vein_02-512x512");
        MonolithSampler = new Tex(Path.Combine(TextureRoot, "Particles", "GigasConsecratedMonolith"), previewLocal: true);
        CrimsonFlowSampler = new Tex("Turbulence_05-512x512");
        ElandPrimarySampler = new Tex("Voronoi_10-512x512");
        ElandDetailSampler = new Tex("T_CloudNoise_Tiled");
    }

    // Palettes, copied from the C# helper.
    static readonly V3 FlameDark = C(6, 0, 3);
    static readonly V3 FlameMid = C(198, 14, 30);
    static readonly V3 FlameCore = C(255, 132, 86);
    static readonly V3 BoltDark = C(26, 0, 6);
    static readonly V3 BoltMid = C(206, 16, 34);
    static readonly V3 BoltCore = C(255, 138, 110);

    static (V3, float) DestinedDeathFlame(V2 uv, float Time, float Progress, float Opacity,
        float Intensity, float Direction)
    {
        const float GroundLine = 0.74f, InvAbove = 1.351f, InvBelow = 3.846f;
        float x = uv.x + Direction * 0.137f;

        float macroN = MacroSampler.R(new V2(x * 2.90f - Time * 0.13f, uv.y * 1.05f - Time * 0.62f));
        float fineN = DetailSampler.R(new V2(x * 5.40f + Time * 0.22f, uv.y * 1.95f - Time * 1.08f));

        float shape = sat(macroN * 1.30f - 0.18f);
        float fire = sat(macroN * 0.70f + fineN * 0.52f - 0.10f);

        float endT = sat(uv.x * 2.6f) * sat((1.0f - uv.x) * 2.6f);
        float span = endT * endT * sat(endT * 2.2f + shape * 0.9f - 0.28f);
        float surge = 0.42f + Progress * 0.58f;

        float d = uv.y - GroundLine;
        float t = sat(-d * InvAbove) + sat(d * InvBelow);
        float belowScale = 1.0f - sat(d * 400.0f) * 0.4f;
        float reach = (0.30f + shape * 0.54f) * belowScale * (0.32f + span * 0.68f) * surge;
        float body = sat((reach - t) * 6.5f) * span;

        float ember = sat(fire * 1.45f - 0.34f);
        float core = sat((reach * 0.46f - t) * 8.0f) * span * ember;

        float emit = Opacity * Intensity;
        float alpha = sat(body * 0.94f) * Opacity;
        V3 rgb = lerp(FlameDark, FlameMid, ember * ember) * alpha
            + (FlameMid * (body * ember * 0.46f) + FlameCore * (core * 0.85f)) * emit;
        return (rgb, alpha);
    }

    static (V3, float) DestinedDeathSeal(V2 uv, float Time, float Progress, float Opacity, float Intensity)
    {
        V2 p = uv - 0.5f;
        float r = length(p) * 2.0f;

        float expand = 1.0f - Progress * 0.42f;
        float macroN = MacroSampler.R(p * (expand * 3.10f) + (0.5f + Time * 0.05f));
        float fineN = DetailSampler.R(p * (expand * 6.20f) + (0.5f - Time * 0.09f));
        float churn = sat(macroN * 0.78f + fineN * 0.46f - 0.14f);

        float grow = Progress * (2.0f - Progress);
        float edge = 0.10f + grow * 0.80f;
        float front = edge + (churn - 0.5f) * 0.10f;

        float quadFade = sat((1.0f - r) * 4.0f);
        quadFade *= quadFade;

        float filled = sat((front - r) * 7.0f) * quadFade;
        float rim = sat(1.0f - abs(r - front) * 9.0f) * quadFade;

        float heat = sat(r * 1.20f);
        float ember = filled * sat(churn * 1.25f - 0.30f) * (0.20f + heat * heat * 1.30f);

        float alpha = sat(filled * (0.88f + churn * 0.22f) + rim * 0.55f) * Opacity;
        V3 rgb = FlameDark * alpha
            + (FlameMid * (ember * 0.90f)
                + FlameCore * (rim * (0.30f + grow * 0.95f) * sat(churn * 1.30f + 0.10f)))
            * (Opacity * Intensity);
        return (rgb, alpha);
    }

    static (V3, float) DestinedDeathBlast(V2 uv, float Time, float Progress, float Opacity, float Intensity)
    {
        V2 p = uv - 0.5f;
        float r = length(p) * 2.0f;

        float expand = 1.0f - Progress * 0.50f;
        float macroN = MacroSampler.R(p * (expand * 2.60f) + (0.5f - Time * 0.12f));
        float fineN = DetailSampler.R(p * (expand * 5.50f) + (0.5f + Time * 0.18f));
        float churn = sat(macroN * 0.82f + fineN * 0.48f - 0.12f);

        float grow = Progress * (2.0f - Progress);
        float front = 0.26f + grow * 0.66f + (churn - 0.5f) * 0.12f;

        float quadFade = sat((1.0f - r) * 3.4f);
        quadFade *= quadFade;

        float ball = sat((front - r) * 4.4f) * quadFade;
        float shell = sat(1.0f - abs(r - front) * 5.2f) * quadFade;
        float ember = sat(churn * 1.35f - 0.26f);
        float heart = sat((front * 0.42f - r) * 3.6f) * quadFade;

        float alpha = sat(ball * (0.84f + ember * 0.26f) + shell * 0.52f) * Opacity;
        V3 rgb = lerp(FlameDark, FlameMid, ember * ember) * alpha
            + (FlameMid * (shell * 0.72f + ball * ember * 0.45f)
                + FlameCore * (heart * 1.10f)) * (Opacity * Intensity);
        return (rgb, alpha);
    }

    static (V3, float) LightningBolt(V2 uv, float Time, float Progress, float Opacity,
        float Active, float Direction)
    {
        float along = uv.x;
        float across = uv.y - 0.5f;

        float coarse = MacroSampler.R(new V2(along * 2.60f - Time * 1.35f, Direction * 0.37f));
        float fine = DetailSampler.R(new V2(along * 7.40f + Time * 2.10f, Direction * 0.61f));

        float taper = sat(along * 9.0f) * sat((1.0f - along) * 3.0f);
        float jitter = ((coarse - 0.5f) * 0.60f + (fine - 0.5f) * 0.30f) * (0.30f + Active * 0.14f);
        float d = abs(across - jitter * taper);

        float glow = sat((0.26f - d) * 4.6f) * taper;
        float body = glow * glow * (0.45f + coarse * 0.55f);
        float core = sat((0.055f + Active * 0.045f - d) * 22.0f) * taper;
        float reveal = 0.35f + Progress * 0.65f;
        float flicker = 0.72f + 0.28f * fine;

        float alpha = sat(body * (0.30f + Active * 0.55f) + core * (0.55f + Active * 0.45f))
            * Opacity * reveal;
        V3 rgb = BoltDark * (body * 0.9f)
            + BoltMid * (body * (0.85f + Active * 0.95f) * flicker)
            + BoltCore * (core * (0.60f + Active * 1.25f));
        return (rgb * (Opacity * reveal), alpha);
    }

    static (V3, float) StormHerald(V2 uv, float Time, float Progress, float Opacity)
    {
        V2 p = uv - 0.5f;
        float r = length(p) * 2.0f;

        float storm = MacroSampler.R(p * 2.40f + (0.5f - Time * 0.06f));
        float arcs = DetailSampler.R(p * (2.90f - r * 0.90f) + (0.5f + Time * 0.115f));

        float bloom = Progress * (2.0f - Progress);
        float radius = 0.30f + bloom * 0.56f;
        float quadFade = sat((1.0f - r) * 3.2f);
        quadFade *= quadFade;
        float cloud = sat((radius + (storm - 0.5f) * 0.14f - r) * 3.2f) * quadFade;

        float crack = sat(1.0f - abs(arcs - 0.52f) * 12.0f) * sat(storm * 1.55f - 0.28f);
        float filament = cloud * crack * (0.30f + bloom * 1.00f);
        float rim = sat(1.0f - abs(r - radius) * 6.5f) * quadFade * storm;
        float eye = sat((0.20f - r) * 5.0f) * bloom;

        float alpha = sat(cloud * (0.78f + storm * 0.28f) + rim * 0.35f) * Opacity;
        V3 rgb = FlameDark * alpha
            + (FlameMid * (filament * 1.05f + rim * 0.60f + eye * 0.50f)
                + FlameCore * (filament * filament * 1.25f + eye * eye * 0.85f)) * Opacity;
        return (rgb, alpha);
    }

    // ---- Effects/GigasSunPillar.fx -----------------------------------------------------------
    static (V3, float) GigasSunPillar(V2 uv, float Time, float Progress, float Active, float Opacity)
    {
        float across = abs(uv.x - 0.5f);
        float macro = MacroSampler.R(new V2(uv.x * 2.7f - Time * 0.06f, uv.y * 1.15f - Time * 0.46f));
        float detail = DetailSampler.R(new V2(uv.x * 7.1f + Time * 0.19f, uv.y * 2.25f + Time * 0.92f));
        float shape = sat(macro * 1.22f - 0.16f);
        float shimmer = sat(macro * 0.68f + detail * 0.52f - 0.12f);
        float reach = 0.240f + shape * 0.035f;
        float body = sat((reach - across) * 8.5f);
        float core = sat((0.052f - across) * 24.0f) * (0.70f + detail * 0.30f);
        float halo = sat((reach + 0.055f - across) * 4.6f) * (0.45f + shimmer * 0.55f);
        float telegraphReveal = sat((Progress - (1.0f - uv.y)) * 9.0f);
        float activeReveal = telegraphReveal + (1.0f - telegraphReveal) * Active;
        float axialFade = sat(uv.y * 18.0f) * sat((1.0f - uv.y) * 7.0f);
        float telegraphHeat = body * (0.13f + shimmer * 0.13f) + halo * 0.07f;
        float strikeHeat = body * (0.50f + shimmer * 0.42f) + core * 0.92f + halo * 0.17f;
        float heat = (telegraphHeat + (strikeHeat - telegraphHeat) * Active) * activeReveal * axialFade;
        V3 color = lerp(C(112, 69, 8), C(255, 180, 35), sat(body * 0.72f + shimmer * 0.28f));
        color = lerp(color, C(255, 243, 172), core * Active);
        float alpha = sat((body * 0.72f + halo * 0.23f + core * 0.48f) * activeReveal * axialFade) * Opacity;
        V3 emission = C(255, 243, 172) * (core * Active * Opacity * 0.34f);
        return (color * alpha + emission, alpha);
    }

    // ---- Effects/GigasSweepBeam.fx -----------------------------------------------------------
    static (V3, float) GigasSweepBeam(V2 uv, float Time, float Progress, float Active, float Opacity, float Direction)
    {
        float along = uv.x;
        float across = abs(uv.y - 0.5f);
        float macro = MacroSampler.R(new V2(along * 1.15f - Time * 0.82f * Direction, uv.y * 2.55f + Time * 0.10f));
        float detail = DetailSampler.R(new V2(along * 5.30f + Time * 2.10f * Direction, uv.y * 6.40f - Time * 0.34f));
        float shimmer = sat(macro * 0.66f + detail * 0.58f - 0.12f);
        float body = sat((0.333f - across) * 7.8f) * (0.62f + shimmer * 0.38f);
        float coreOffset = (macro - 0.5f) * 0.050f;
        float core = sat((0.075f - abs(uv.y - 0.5f - coreOffset)) * 19.0f);
        float halo = sat((0.460f - across) * 5.0f) * (0.34f + shimmer * 0.66f);
        float endLimit = 0.4787f - across * across * 0.24f;
        float endFade = sat((endLimit - abs(along - 0.5f)) * 72.0f);
        body *= endFade;
        core *= endFade;
        halo *= endFade;
        float gather = Progress;
        float telegraphHeat = halo * 0.08f + core * 0.18f;
        float strikeHeat = body * (0.56f + shimmer * 0.46f) + core * 0.94f + halo * 0.15f;
        float heat = (telegraphHeat + (strikeHeat - telegraphHeat) * Active) * gather;
        V3 color = lerp(C(105, 61, 5), C(255, 174, 21), sat(body * 0.73f + shimmer * 0.27f));
        color = lerp(color, C(255, 244, 183), core * Active);
        float alpha = sat((body * (0.70f + Active * 0.22f) + halo * 0.18f + core * 0.45f) * gather) * Opacity;
        V3 emission = C(255, 244, 183) * (core * Active * Opacity * 0.32f);
        return (color * (heat * Opacity) + emission, alpha);
    }

    // ---- Effects/GigasNovaRing.fx ------------------------------------------------------------
    static (V3, float) GigasNovaSun(V2 uv, float Time, float Opacity, bool pixelated = true)
    {
        if (pixelated) uv = PixelateShaderUV(uv, 616, 616);
        float radius = length((uv - 0.5f) * 616f);
        float macro = SolarMacroSampler.R(uv * 1.75f + new V2(-Time * 0.10f, Time * 0.14f));
        float detail = SolarDetailSampler.R(uv * 5.20f + new V2(Time * 0.23f, -Time * 0.31f));
        float flame = sat(macro * 0.68f + detail * 0.56f - 0.12f);
        float safeField = sat((270f - radius) / 12f) * (0.26f + flame * 0.58f);
        float solarReach = 270f + (macro - 0.48f) * 74f;
        float solarField = sat((solarReach - radius) / 13f) * (0.18f + flame * 0.62f);
        float corona = solarField * sat((radius - 270f + 11f) / 26f);
        float edge = sat((8f - abs(radius - 270f)) / 5f) * (0.34f + flame * 0.36f);
        float hotPockets = safeField * sat(flame * 1.32f - 0.16f);
        V3 color = lerp(C(105, 61, 5), C(255, 176, 25), sat(safeField * 0.70f + flame * 0.30f));
        color = lerp(color, C(255, 245, 190), hotPockets * 0.48f);
        float alpha = sat(safeField * 0.68f + corona * 0.38f + edge * 0.28f) * Opacity;
        return (color * alpha + C(255, 245, 190) * (hotPockets * 0.17f + edge * 0.04f) * Opacity, alpha);
    }

    static (V3, float) GigasNovaCorona(V2 uv, float Time, float Opacity, bool pixelated = true)
    {
        if (pixelated) uv = PixelateShaderUV(uv, 768, 768);
        float radius = length((uv - 0.5f) * 768f);
        float macro = SolarMacroSampler.R(uv * 1.18f + new V2(Time * 0.045f, -Time * 0.072f));
        float flame = SolarFlameSampler.R(uv * 0.94f + new V2(-Time * 0.035f, Time * 0.235f));
        float tongues = sat(flame * 1.24f + macro * 0.46f - 0.23f);
        float reach = 270f + 18f + tongues * 74f;
        float crown = sat((reach - radius) / 10f) * sat((radius - 270f + 18f) / 14f);
        float rim = sat((10f - abs(radius - 270f)) / 6f);
        float heat = sat(tongues * 1.32f - 0.12f);
        V3 color = lerp(C(105, 61, 5), C(255, 176, 25), heat);
        color = lerp(color, C(255, 245, 190), heat * heat * 0.42f);
        float alpha = sat(crown * (0.22f + heat * 0.36f) + rim * 0.10f) * Opacity;
        return (color * alpha + C(255, 245, 190) * crown * heat * 0.11f * Opacity, alpha);
    }

    static (V3, float) GigasNovaField(V2 uv, float Time, float Progress, float Active, float Opacity, bool pixelated = true)
    {
        if (Active == 0f)
        {
            if (pixelated) uv = PixelateShaderUV(uv, 572, 572);
            float radius = length((uv - 0.5f) * 572f);
            float edge = sat((8f - abs(radius - 270f * Progress)) / 5f);
            float fill = sat((270f * Progress - radius) / 16f) * 0.09f;
            float alpha = (fill + edge * 0.42f) * Opacity;
            return (C(238, 161, 24) * alpha, alpha);
        }

        V2 bodyUV = new((uv.x - 0.5f) / (616f / 768f) + 0.5f, (uv.y - 0.5f) / (616f / 768f) + 0.5f);
        var (coronaRgb, coronaAlpha) = GigasNovaCorona(uv, Time, Opacity, pixelated);
        if (bodyUV.x < 0f || bodyUV.x > 1f || bodyUV.y < 0f || bodyUV.y > 1f) return (coronaRgb, coronaAlpha);
        var (bodyRgb, bodyAlpha) = GigasNovaSun(bodyUV, Time, Opacity, pixelated);
        return (coronaRgb + bodyRgb * (1f - coronaAlpha), coronaAlpha + bodyAlpha * (1f - coronaAlpha));
    }

    // ---- Candidate: GigasLightHand ------------------------------------------------------------
    // Offline-only design study. Two broken stone monoliths rise from the ground and close toward
    // the future seam. Slow light migrates inside their fractures; only the true clap seam goes hot.
    static (V3, float) GigasLightHand(V2 uv, float Time, float Progress, float Active)
    {
        const float panelW = 320f, panelH = 180f, bottom = 150f, fullHeight = 130f;
        const float slabWidth = 74f, seamDrawWidth = 94f, seamWidth = 70f;
        float x = (uv.x - 0.5f) * panelW;
        float rise = MathF.Min(1f, Progress * 2f);
        float height = fullHeight * rise;
        float offset = 100f + (12f - 100f) * Progress * Progress;
        float opacity = Active > 0f ? .96f : .32f + Progress * .42f;
        V3 rgb = new(0, 0, 0); float alpha = 0f;

        for (int side = -1; side <= 1; side += 2)
        {
            float dx = x - side * offset;
            float vertical = bottom - uv.y * panelH;
            float localU = dx / slabWidth + 0.5f;
            float localV = 1f - vertical / MathF.Max(height, 1f);
            float sampleU = side > 0 ? 1f - localU : localU;
            V4 slab = localU < 0f || localU > 1f || localV < 0f || localV > 1f
                ? new V4(0, 0, 0, 0) : MonolithSampler.T(sampleU, localV);
            float flow = SolarMacroSampler.R(new V2(localU * 1.35f - Time * 0.045f, localV * 2.05f + Time * 0.17f));
            float crack = CrackSampler.R(new V2(localU * 2.65f + Time * 0.016f, localV * 3.40f - Time * 0.025f));
            float innerFace = sat((-side * dx / slabWidth + .14f) * 4.2f);
            float fracture = sat((.30f - crack) * 4.4f) * sat(flow * 1.42f - .27f) * slab.a;
            float faceLight = slab.a * innerFace * (.05f + flow * .12f);
            V3 stone = new V3(slab.x, slab.y, slab.z) * (.78f + flow * .42f) * opacity;
            V3 gold = C(244, 166, 26) * (fracture * .74f + faceLight) * opacity;
            V3 emission = C(255, 246, 196) * fracture * flow * .13f * opacity;
            float a = slab.a * opacity;
            rgb = stone + gold + emission + rgb * (1f - a);
            alpha = a + alpha * (1f - a);
        }

        if (Active > 0f)
        {
            float vertical = bottom - uv.y * panelH;
            float sx = x / seamDrawWidth + .5f;
            float sy = 1f - vertical / fullHeight;
            float flow = SolarMacroSampler.R(new V2(sx * 1.15f + Time * .08f, sy * 2.30f - Time * .29f));
            float crack = CrackSampler.R(new V2(sx * 2.40f - Time * .024f, sy * 3.10f + Time * .038f));
            float coreHalfWidth = seamWidth * .5f + (flow - .5f) * 4f;
            float core = sat((coreHalfWidth - abs(x)) / 4f);
            float halo = sat((coreHalfWidth + 12f - abs(x)) / 8f);
            float agitation = sat(flow * .72f + (1f - crack) * .34f);
            float a = sat(core * (.72f + agitation * .20f) + halo * .18f) * .92f;
            V3 color = lerp(C(244, 166, 26), C(255, 246, 196), core * (.52f + agitation * .38f));
            V3 emission = C(255, 246, 196) * core * agitation * .18f * .92f;
            rgb = color * a + emission + rgb * (1f - a);
            alpha = a + alpha * (1f - a);
        }
        return (rgb, alpha);
    }

    // ---- Candidate: GigasLightHandRich -------------------------------------------------------
    // Keeps the translucent 2x2 monolith sprite as stone, then gives it an uneven inner-face glow,
    // moving buried heat, and fractured rim glints. The bright compression seam remains distinct.
    static (V3, float) GigasLightHandRich(V2 uv, float Time, float Progress, float Active)
    {
        const float panelW = 320f, panelH = 180f, bottom = 150f, fullHeight = 130f;
        const float slabWidth = 74f, seamDrawWidth = 94f, seamWidth = 70f;
        float x = (uv.x - .5f) * panelW;
        float rise = MathF.Min(1f, Progress * 2f);
        float height = fullHeight * rise;
        float offset = 100f + (12f - 100f) * Progress * Progress;
        float opacity = Active > 0f ? .96f : .32f + Progress * .42f;
        V3 rgb = new(0, 0, 0); float alpha = 0f;

        for (int side = -1; side <= 1; side += 2)
        {
            float dx = x - side * offset;
            float vertical = bottom - uv.y * panelH;
            float localU = dx / slabWidth + .5f;
            float localV = 1f - vertical / MathF.Max(height, 1f);
            float sampleU = side > 0 ? 1f - localU : localU;
            V4 slab = localU < 0f || localU > 1f || localV < 0f || localV > 1f
                ? new V4(0, 0, 0, 0) : MonolithSampler.T(sampleU, localV);

            float flowA = SolarMacroSampler.R(new V2(localU * 1.18f - Time * .052f, localV * 1.82f + Time * .21f));
            float flowB = SolarDetailSampler.R(new V2(localU * 4.65f + Time * .17f, localV * 2.65f - Time * .38f));
            float crack = CrackSampler.R(new V2(localU * 2.45f + Time * .021f, localV * 3.15f - Time * .032f));
            float flow = sat(flowA * .66f + flowB * .48f - .12f);
            float innerFace = sat((-side * dx / slabWidth + .14f) * 4.2f);

            // The sprite already owns the jagged outline. These local bands make those chipped
            // edges catch and release light rather than becoming a uniform bright rectangle.
            float sideRim = sat((.13f - MathF.Min(localU, 1f - localU)) * 8f);
            float topRim = sat((.12f - localV) * 8f);
            float rim = slab.a * sat(sideRim + topRim * .58f) * sat(flowB * .72f + flowA * .48f - .14f);
            float veins = slab.a * sat((.38f - crack) * 3.4f) * sat(flow * 1.30f - .18f);
            float strata = slab.a * sat(flowA * .74f + flowB * .42f - .24f);
            float buriedGlow = slab.a * innerFace * (.08f + strata * .18f);

            V3 stone = new V3(slab.x, slab.y, slab.z) * (.38f + flow * .29f + innerFace * .18f) * opacity;
            V3 gold = C(218, 139, 19) * (veins * .88f + buriedGlow * 1.35f + rim * .44f) * opacity;
            V3 hot = C(255, 235, 164) * (veins * .25f + rim * .30f) * opacity;
            float a = slab.a * opacity;
            rgb = stone + gold + hot + rgb * (1f - a);
            alpha = a + alpha * (1f - a);
        }

        if (Active > 0f)
        {
            float vertical = bottom - uv.y * panelH;
            float sx = x / seamDrawWidth + .5f;
            float sy = 1f - vertical / fullHeight;
            float flow = SolarMacroSampler.R(new V2(sx * 1.15f + Time * .08f, sy * 2.30f - Time * .29f));
            float crack = CrackSampler.R(new V2(sx * 2.40f - Time * .024f, sy * 3.10f + Time * .038f));
            float coreHalfWidth = seamWidth * .5f + (flow - .5f) * 4f;
            float core = sat((coreHalfWidth - abs(x)) / 4f);
            float halo = sat((coreHalfWidth + 12f - abs(x)) / 8f);
            float agitation = sat(flow * .72f + (1f - crack) * .34f);
            float a = sat(core * (.72f + agitation * .20f) + halo * .18f) * .92f;
            V3 color = lerp(C(244, 166, 26), C(255, 246, 196), core * (.52f + agitation * .38f));
            V3 emission = C(255, 246, 196) * core * agitation * .18f * .92f;
            rgb = color * a + emission + rgb * (1f - a);
            alpha = a + alpha * (1f - a);
        }
        return (rgb, alpha);
    }

    // ---- Candidate: GigasLightHandLuminous ---------------------------------------------------
    // The stone monolith is deliberately only a translucent foreground silhouette. The actual
    // effect is a larger, low-alpha field of moving holy light behind it: the sampled silhouette
    // keeps the chipped profile while neighbouring samples turn that profile into a soft halo.
    // At contact the existing Nova shader replaces the old rectangular compression seam.
    static (V3, float) GigasLightHandLuminous(V2 uv, float Time, float Progress, float Active)
    {
        const float panelW = 320f, panelH = 180f, bottom = 151f, fullHeight = 130f;
        const float slabWidth = 74f, auraWidth = 116f, auraHeight = 154f;
        float x = (uv.x - .5f) * panelW;
        float pixelY = uv.y * panelH;
        float rise = MathF.Min(1f, Progress * 2f);
        float height = fullHeight * rise;
        float offset = 100f + (13f - 100f) * Progress * Progress;
        V3 rgb = new(0, 0, 0); float alpha = 0f;

        // The compact, decorative nova sits behind the slabs. It is intentionally much smaller
        // than Wrath of Gold's gameplay field: it describes the impact, not a new damage shape.
        if (Active > 0f)
        {
            float burstU = x / 142f + .5f;
            float burstV = (pixelY - (bottom - fullHeight * .52f)) / 142f + .5f;
            if (burstU >= 0f && burstU <= 1f && burstV >= 0f && burstV <= 1f)
            {
                var (burstRgb, burstAlpha) = GigasNovaField(new V2(burstU, burstV), Time, 1f, 1f, .68f);
                rgb = burstRgb;
                alpha = burstAlpha;
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float center = side * offset;
            float auraU = (x - center) / auraWidth + .5f;
            float auraV = 1f - (bottom - pixelY) / (MathF.Max(height, 1f) * auraHeight / fullHeight);
            float spriteU = (auraU - .5f) * auraWidth / slabWidth + .5f;
            float spriteV = (auraV - .5f) * auraHeight / fullHeight + .5f;
            float sampleU = side > 0 ? 1f - spriteU : spriteU;
            bool inAura = spriteU >= 0f && spriteU <= 1f && spriteV >= 0f && spriteV <= 1f;
            V4 auraSlab = inAura ? MonolithSampler.T(sampleU, spriteV) : new V4(0, 0, 0, 0);

            // Four nearby alpha reads approximate the LinearClamp blur used by the in-game aura
            // pass. They inherit the jagged sprite contour but never produce a hard rectangular edge.
            float softMask = auraSlab.a;
            if (inAura)
            {
                float left = spriteU > .070f ? MonolithSampler.T(side > 0 ? 1f - (spriteU - .070f) : spriteU - .070f, spriteV).a : 0f;
                float right = spriteU < .930f ? MonolithSampler.T(side > 0 ? 1f - (spriteU + .070f) : spriteU + .070f, spriteV).a : 0f;
                float up = spriteV > .045f ? MonolithSampler.T(sampleU, spriteV - .045f).a : 0f;
                float down = spriteV < .955f ? MonolithSampler.T(sampleU, spriteV + .045f).a : 0f;
                softMask = (softMask * 1.5f + left + right + up + down) / 5.5f;
            }

            float macro = SolarMacroSampler.R(new V2(auraU * 1.22f - Time * .075f, auraV * 1.78f + Time * .24f));
            float detail = SolarDetailSampler.R(new V2(auraU * 4.80f + Time * .26f, auraV * 3.20f - Time * .42f));
            float flame = sat(macro * .65f + detail * .54f - .17f);
            float upwardTongues = sat(detail * 1.34f + macro * .42f - .30f);
            float interior = softMask * (.62f + flame * .31f);
            float outerFade = softMask * (.18f + upwardTongues * .26f);
            float edgeLight = softMask * (1f - auraSlab.a) * upwardTongues;
            float heat = auraSlab.a * sat(flame * 1.24f - .13f);
            float auraAlpha = sat(interior + outerFade + edgeLight * .52f) * (.82f + Active * .12f);
            V3 auraColor = lerp(C(187, 108, 12), C(255, 225, 137), flame);
            auraColor = lerp(auraColor, C(255, 249, 211), heat * .62f);
            V3 auraRgb = auraColor * auraAlpha + C(255, 246, 193) * (interior * .28f + heat * .54f + edgeLight * .42f);
            rgb = auraRgb + rgb * (1f - auraAlpha);
            alpha = auraAlpha + alpha * (1f - auraAlpha);

            // The real 2x2 monolith remains on top, translucent enough that the moving light is
            // still the thing the player reads first.
            float localU = (x - center) / slabWidth + .5f;
            float localV = 1f - (bottom - pixelY) / MathF.Max(height, 1f);
            float foregroundU = side > 0 ? 1f - localU : localU;
            bool inSprite = localU >= 0f && localU <= 1f && localV >= 0f && localV <= 1f;
            V4 slab = inSprite ? MonolithSampler.T(foregroundU, localV) : new V4(0, 0, 0, 0);
            float spriteGlow = slab.a * sat(macro * .58f + detail * .36f);
            float spriteAlpha = slab.a * .22f;
            V3 spriteRgb = new V3(slab.x, slab.y, slab.z) * .25f
                + C(255, 225, 130) * spriteGlow * .24f;
            rgb = spriteRgb * spriteAlpha + rgb * (1f - spriteAlpha);
            alpha = spriteAlpha + alpha * (1f - spriteAlpha);
        }
        return (rgb, alpha);
    }

    // ---- Candidate: GigasSolarBoulder ---------------------------------------------------------
    // A rough, furnace-dark miniature sun. Ordinary UV noise gives it volume; the noise only
    // modulates the edge, never decides it, and quadFade is provably zero before the square quad.
    static (V3, float) GigasSolarBoulder(V2 uv, float Time, float Progress)
    {
        V2 p = (uv - .5f) * 2f;
        float r = length(p);
        float macro = SolarMacroSampler.R(p * 2.15f + new V2(.5f - Time * .055f, .5f + Time * .071f));
        float detail = SolarDetailSampler.R(p * 5.65f + new V2(.5f + Time * .19f, .5f - Time * .24f));
        float flame = sat(macro * .72f + detail * .56f - .12f);

        // reach is at most .56 and quadFade is zero at r = 1, so no flat quad cutoff is possible.
        float quadFade = sat((1f - r) * 4.2f);
        quadFade *= quadFade;
        float reach = .43f + (macro - .5f) * .26f;
        float body = sat((reach - r) * 7.2f) * quadFade;
        float coronaReach = .61f + (macro - .5f) * .34f;
        float coronaBand = sat((coronaReach - r) * 5.2f) * sat((r - reach * .64f) * 8.4f) * quadFade;
        float corona = coronaBand * flame;
        float molten = body * flame;
        float core = sat((.18f - r) * 8.2f);
        float pulse = .78f + Progress * .22f;

        float alpha = sat(body * (.72f + molten * .18f) + corona * .34f + core * .32f) * pulse;
        V3 stone = lerp(C(43, 23, 10), C(118, 62, 9), molten * .56f);
        V3 rgb = stone * (body * .82f * pulse)
            + C(246, 137, 16) * (molten * .72f + corona * .32f) * pulse
            + C(255, 239, 166) * core * .38f * pulse;
        return (rgb, alpha);
    }

    // ---- Candidate: GigasHaloSun --------------------------------------------------------------
    // Small votive suns. The core stays inside the 20px hitbox; a low-alpha, irregular
    // corona is decorative light outside it. Phase offsets will differ per halo slot in game.
    static (V3, float) GigasHaloSun(V2 uv, float Time, float Phase, float Active)
    {
        V2 p = (uv - .5f) * 2f;
        float r = length(p);
        float macro = SolarMacroSampler.R(p * 2.85f + new V2(.5f + Time * .08f + Phase, .5f - Time * .11f));
        float detail = SolarDetailSampler.R(p * 7.10f + new V2(.5f - Time * .21f, .5f + Time * .17f + Phase));
        float fire = sat(macro * .70f + detail * .54f - .14f);
        float quadFade = sat((1f - r) * 4.6f); quadFade *= quadFade;

        // Bright core radius .18 = 10px in the 56px shell (the 20px hostile body). Both wider
        // layers must still die before r=1, independent of their noise.
        float core = sat((.18f - r) * 11f) * (.64f + detail * .36f);
        float bodyReach = .30f + (macro - .5f) * .13f;
        float body = sat((bodyReach - r) * 9f) * quadFade;
        float coronaReach = .52f + (macro - .5f) * .24f;
        float corona = sat((coronaReach - r) * 6f) * sat((r - bodyReach * .42f) * 8f) * fire * quadFade;
        float launch = .58f + Active * .42f;
        float alpha = sat(body * (.66f + fire * .18f) + corona * .30f + core * .48f) * launch;
        V3 color = lerp(C(89, 48, 5), C(248, 159, 22), sat(body * .60f + fire * .40f));
        color = lerp(color, C(255, 244, 179), core * .72f);
        V3 emission = C(255, 244, 179) * (core * (.16f + Active * .13f)) * launch;
        return (color * alpha + emission, alpha);
    }

    // ---- Candidate: GigasConsecratedGround ---------------------------------------------------
    // One of the Solar Boulder fire modules: tall bright-yellow tongues with a fade that continues
    // into the ground padding. The real caller is 52x180, with its lower padding occluded by terrain.
    static (V3, float) GigasConsecratedGround(V2 uv, float Time, float Remaining, bool pixelated = true)
    {
        if (pixelated)
        {
            uv = PixelateShaderUV(uv, 52, 180);
        }
        float macro = SolarMacroSampler.R(new V2(uv.x * 1.10f - Time * .08f, uv.y * .72f + Time * .13f));
        float detail = SolarDetailSampler.R(new V2(uv.x * 4.40f + Time * .25f, uv.y * 1.70f - Time * .52f));
        float flameHalfWidth = (.04f + .30f * sat(uv.y * 1.45f))
            * (.86f + macro * .18f) * sat((1f - uv.y) * 5f);
        float flameBody = sat((flameHalfWidth - abs(uv.x - .5f)) * 12f);
        float emberBottom = .78f + macro * .08f;
        float floorFade = sat((emberBottom - uv.y) * 10f);
        float tongueTop = .76f - macro * .70f;
        float tongues = sat((uv.y - tongueTop) * 6.8f) * floorFade * flameBody;
        float emberBed = sat((uv.y - .60f) * 7.6f) * floorFade * flameBody;

        float coals = tongues * (.62f + macro * .38f);
        float hotCracks = emberBed * sat((detail - .20f) * 1.25f) * (.45f + macro * .55f);

        float age = Remaining;
        float alpha = sat(coals * .92f + hotCracks * .54f) * age;
        V3 color = lerp(C(92, 50, 3), C(255, 183, 20), sat(coals * .56f + macro * .44f));
        color = lerp(color, C(255, 249, 190), hotCracks * .82f);
        return (color * alpha, alpha);
    }

    // ---- Candidate: GigasHeavenlySpear -------------------------------------------------------
    // A sacred lance with a sharp, small head at the true 18px damage point and a living fire wake
    // behind it. The wake grows by wave tier in C#; it is explicitly softer than the spearhead.
    static (V3, float) GigasHeavenlySpear(V2 uv, float Time, float Progress, float Active)
    {
        float along = uv.x;
        float across = abs(uv.y - .5f);
        float macro = SolarMacroSampler.R(new V2(along * 2.20f - Time * .20f, uv.y * 1.45f + Time * .09f));
        float detail = SolarDetailSampler.R(new V2(along * 6.65f + Time * .31f, uv.y * 3.10f - Time * .48f));

        // The tail and tip both reach zero inside the quad. The tip is a genuine taper, rather
        // than a rectangular beam, while macro noise only perturbs its already-solid silhouette.
        float tail = sat(along * 27f);
        float tipFade = sat((1f - along) * 27f);
        float tailBlend = sat(along * 5.5f);
        float shaftWidth = (.040f + macro * .055f) * tailBlend;
        // A broad head occupies .66-.93, then a distinct needle projects forward from it.
        // This lets the oncoming end keep its heavy mass without reading as a blunt laser cap.
        float head = sat((along - .66f) * 5.5f) * sat((.93f - along) * 7.2f);
        float needleBlend = sat((along - .84f) * 6.25f);
        float headWidth = shaftWidth + head * (.145f + macro * .020f);
        float needleWidth = (1f - along) * .50f;
        float width = headWidth * (1f - needleBlend) + needleWidth * needleBlend;
        float body = sat((width - across) * 22f) * tail * tipFade;
        float wakeNoise = sat(macro * .66f + detail * .54f - .14f);
        float wake = sat((shaftWidth + .040f + macro * .050f - across) * 8f) * tail * tipFade * (1f - head) * wakeNoise;

        // During the harmless hover it builds outward from the head; on launch Active fully
        // reveals it. This matches the existing inward GoldCoin gathering without changing it.
        float forming = sat((Progress - (1f - along) * .74f) * 5.2f);
        float reveal = forming * (1f - Active) + Active;
        float heat = sat(macro * .64f + detail * .52f - .13f);

        // The base pass deliberately stops before the narrow core and fissures. Those are restored
        // exactly by the detail pass below, preserving the old preview's division of visual weight.
        float alpha = sat(body * (.76f + heat * .16f) + wake * .30f) * reveal;
        V3 color = lerp(C(57, 31, 4), C(229, 142, 16), sat(body * .48f + heat * .52f));
        return (color * alpha, alpha);
    }

    // Second transparent pass: the detail terms from the preferred over-budget preview, isolated
    // so they can retain their motion, narrow core, and emissive head without compromising Reach.
    static (V3, float) GigasHeavenlySpearDetails(V2 uv, float Time, float Progress, float Active)
    {
        float along = uv.x;
        float across = abs(uv.y - .5f);
        float macro = SolarMacroSampler.R(new V2(along * 2.20f - Time * .20f, uv.y * 1.45f + Time * .09f));
        float detail = SolarDetailSampler.R(new V2(along * 6.65f + Time * .31f, uv.y * 3.10f - Time * .48f));
        float tail = sat(along * 27f);
        float tipFade = sat((1f - along) * 27f);
        float tailBlend = sat(along * 5.5f);
        float head = sat((along - .66f) * 5.5f) * sat((.93f - along) * 7.2f);
        float needleBlend = sat((along - .84f) * 6.25f);
        float shaftWidth = (.040f + macro * .055f) * tailBlend;
        float headWidth = shaftWidth + head * (.145f + macro * .020f);
        float needleWidth = (1f - along) * .50f;
        float width = headWidth * (1f - needleBlend) + needleWidth * needleBlend;
        float body = sat((width - across) * 22f) * tail * tipFade;

        float forming = sat((Progress - (1f - along) * .74f) * 5.2f);
        float reveal = forming * (1f - Active) + Active;
        float core = sat((.105f - across) * 16f) * sat((along - .68f) * 4.1f) * tail * tipFade;
        float fissures = body * sat(detail * 1.28f - macro * .26f - .10f) * (1f - needleBlend * .55f);
        float alpha = sat(core * .36f + fissures * .25f) * reveal;
        V3 color = lerp(C(229, 142, 16), C(255, 238, 163), core * .72f + fissures * .25f);
        V3 emission = C(255, 220, 118) * core * (.08f + Active * .12f) * reveal;
        return (color * alpha + emission, alpha);
    }

    static (V3, float) GigasHeavenlySpearLayered(V2 uv, float Time, float Progress, float Active)
    {
        var (baseRgb, baseAlpha) = GigasHeavenlySpear(uv, Time, Progress, Active);
        var (detailRgb, detailAlpha) = GigasHeavenlySpearDetails(uv, Time, Progress, Active);
        return (detailRgb + baseRgb * (1f - detailAlpha), detailAlpha + baseAlpha * (1f - detailAlpha));
    }

    // ---- Effects/RedKnightCrimsonVFX.fx : BombBlastPixel ------------------------------------
    // Samplers per RedKnightVFX.DrawCrimsonQuad: s1 = Turbulence_05-512x512 (FlowSampler),
    // s2 = Grainy_07-512x512 (DetailSampler). BombBlastPixel only uses FlowSampler.
    static Tex CrimsonFlowSampler;

    static (V3, float) BombBlast(V2 uv, float Time, float Progress, float Opacity,
        V3 DarkColor, V3 MidColor, V3 CoreColor)
    {
        V2 p = (uv - 0.5f) * 2.0f;
        float broad = CrimsonFlowSampler.R(uv * 2.35f + new V2(-Time * 0.22f, Time * 0.17f));

        float eased = Progress * (2.0f - Progress);
        float radius = 0.12f + (0.92f - 0.12f) * eased;
        float distortedDistance = length(p) + (broad - 0.5f) * 0.19f;
        float body = sat((radius - distortedDistance) * 5.9f);
        float rollingEdge = sat(1.0f - abs(distortedDistance - radius + 0.05f) * 7.0f)
            * sat(broad * 1.2f - 0.2f);
        float core = sat((radius * 0.46f - distortedDistance) * 6.2f) * (1.0f - Progress * 0.55f);

        V3 color = lerp(DarkColor, MidColor, sat(body * 0.68f + rollingEdge * 0.54f));
        color = lerp(color, CoreColor, core);
        float energy = body * (0.52f + broad * 0.48f) + rollingEdge * 0.86f + core * 1.18f;
        float alpha = sat(body * 0.70f + rollingEdge * 0.82f + core) * Opacity * (1.0f - Progress * 0.78f);
        return (color * energy, alpha);
    }

    /// The three-shell BombExplosionLayered composite from RedKnightVFX.DrawBurst, resolved in
    /// PANEL space so the real anchor offsets are exercised. DrawCrimsonQuad anchors each quad at
    /// its BOTTOM-CENTRE, so a quad of height H is centred H/2 above the point passed in; the
    /// caller shifts each shell by (H - diameter)/2 to keep them concentric. All layers are
    /// BlendState.Additive (DrawCrimsonQuad hardcodes it).
    /// `layered == false` renders only the core, i.e. the old single-quad look, for A/B.
    static (V3, float) BombLayeredComposite(V2 panelUV, float Time, float Progress, bool layered)
    {
        const float diameter = 132f;
        float outerW = diameter * 1.38f, outerH = outerW * 0.92f;   // 182.2 x 167.6
        float innerW = diameter * 0.66f, innerH = innerW * 1.08f;   //  87.1 x  94.1

        // Panel spans the widest shell; its centre is the shared visual centre of all three.
        float panelW = outerW, panelH = outerH;
        V2 d = new((panelUV.x - 0.5f) * panelW, (panelUV.y - 0.5f) * panelH);

        // Accumulate back-to-front in PREMULTIPLIED space, then hand the result to the harness as a
        // single premultiplied-alpha panel. Two different blend contracts are in play, matching the
        // real call:
        //   - an AlphaBlend layer OCCLUDES:  acc.rgb = src.rgb + acc.rgb*(1-src.a);
        //                                    acc.a   = src.a   + acc.a  *(1-src.a)
        //   - an Additive layer only ADDS:   acc.rgb += src.rgb * src.a;  acc.a unchanged
        V3 rgb = new(0, 0, 0);
        float acc = 0f;

        (V3, float) Layer(float w, float h, float dx, float layerProgress, float opacity,
            V3 dark, V3 mid, V3 core)
        {
            V2 luv = new((d.x - dx) / w + 0.5f, d.y / h + 0.5f);
            if (luv.x < 0f || luv.x > 1f || luv.y < 0f || luv.y > 1f) return (new V3(0, 0, 0), 0f);
            return BombBlast(luv, Time, layerProgress, opacity, dark, mid, core);
        }

        if (layered)
        {
            // OUTER shell — BlendState.AlphaBlend, premultiplied. This is the occluding one.
            var (oc, oa) = Layer(outerW, outerH, 0f, sat(Progress + 0.14f), 0.72f,
                C(14, 1, 5), C(104, 12, 12), C(190, 70, 28));
            rgb = oc + rgb * (1f - oa);
            acc = oa + acc * (1f - oa);
        }
        {
            // CORE — Additive, unchanged from the original single-quad blast.
            var (cc, ca) = Layer(diameter, diameter, 0f, Progress, 0.96f,
                C(30, 0, 12), C(244, 42, 25), C(255, 164, 78));
            rgb = rgb + cc * ca;
        }
        if (layered && Progress > 0.18f)
        {
            // INNER late puff — Additive.
            var (ic, ia) = Layer(innerW, innerH, 2f, sat((Progress - 0.18f) / 0.82f), 0.72f,
                C(24, 0, 10), C(244, 42, 25), C(255, 164, 78));
            rgb = rgb + ic * ia;
        }

        return (rgb, acc);
    }

    // ---- Effects/ElandToxicVFX.fx ------------------------------------------------------------
    // Kept in lockstep with ElandToxicVFX: the field and impact use AlphaBlend, so their RGB is
    // explicitly premultiplied here. These panels use the exact draw sizes from EnemyVFX.
    static V2 ElandPixelate(V2 uv, float width, float height)
    {
        float bx = 2f / MathF.Max(width, 1f), by = 2f / MathF.Max(height, 1f);
        return new V2((MathF.Floor(uv.x / bx) + .5f) * bx, (MathF.Floor(uv.y / by) + .5f) * by);
    }

    static (V3, float) ElandToxicField(V2 c, float drawSize, float Time, float Progress, float Active, float Opacity, float direction)
    {
        V2 uv = ElandPixelate(c, drawSize, drawSize);
        float r = length(uv - .5f);
        float n1 = ElandDetailSampler.R(uv * 1.9f + new V2(Time * .031f, -Time * .052f));
        float n2 = ElandDetailSampler.R(uv * 3.7f + new V2(-Time * .043f, Time * .028f));
        float bubbles = ElandPrimarySampler.R(uv * 2.4f + new V2(Time * .017f, -Time * .075f));
        float billow = sat(n1 * .72f + n2 * .54f - .16f);
        float damageEdge = .5f * direction;
        float feather = MathF.Max(.5f - damageEdge, .02f);
        float cloudReach = damageEdge + feather * (.38f + n1 * .70f);
        float damageBody = sat((damageEdge - r) * 8f);
        float cloudBody = sat((cloudReach - r) * 8f);
        float radial = MathF.Max(damageBody * .82f, cloudBody * (.46f + billow * .54f));
        float quadFade = sat((.5f - r) * 9f);
        radial *= quadFade * quadFade;
        float density = sat(radial * (.42f + billow * .95f) + sat(bubbles - .72f) * radial * .55f);
        float d3 = density * density * density;
        V3 color = lerp(C(8, 25, 8), C(66, 176, 54), density);
        color = lerp(color, C(213, 255, 133), d3 * (.35f + Active * .5f));
        float hold = MathF.Min(Progress * 9f, 1f) - MathF.Max(Progress - .72f, 0f) * 3.57f;
        float alpha = density * Opacity * MathF.Max(hold, 0f) * (.5f + Active * .5f);
        return (color * alpha, alpha);
    }

    static (V3, float) ElandPoisonBurstAura(V2 c, float drawSize, float Time, float Progress, float Opacity)
    {
        V2 uv = ElandPixelate(c, drawSize, drawSize);
        float r = length(uv - .5f);
        float n1 = ElandDetailSampler.R(uv * 2.2f + new V2(Time * .045f, -Time * .034f));
        float n2 = ElandDetailSampler.R(uv * 4.1f + new V2(-Time * .071f, Time * .053f));
        float cells = ElandPrimarySampler.R(uv * 3f + new V2(Time * .03f, -Time * .06f));
        float churn = sat(n1 * .70f + n2 * .46f - .14f);
        float body = sat((.38f - r) * 6.4f);
        float outer = sat((.50f - r) * 8f); outer *= outer;
        float density = body * (.46f + churn * .54f + sat(cells - .72f) * .22f) * outer;
        float core = sat((.19f - r) * 8.5f) * (.55f + cells * .45f);
        float fadeIn = MathF.Min(Progress * 12f, 1f);
        float fadeOut = sat((1f - Progress) * 2.22f + r * .001f);
        float life = fadeIn * fadeOut;
        V3 color = lerp(C(8, 25, 8), C(66, 176, 54), density);
        color = lerp(color, C(213, 255, 133), sat(core + density * density * .25f));
        float alpha = sat(density + core * .42f) * Opacity * life;
        float energy = density * .72f + core * 1.05f;
        return (color * (energy * alpha), alpha);
    }

    static (V3, float) ElandVenomGlob(V2 c, float width, float height, float Time, float Opacity)
    {
        V2 uv = ElandPixelate(c, width, height);
        float x = uv.x, y = MathF.Abs(uv.y - .5f);
        float flow = ElandDetailSampler.R(new V2(x * 2.4f - Time * 1.15f, uv.y * 2f + Time * .22f));
        float cells = ElandPrimarySampler.R(uv * new V2(2.6f, 3.4f) + new V2(-Time * .95f, Time * .18f));
        float headField = sat((.34f + flow * .10f - length(new V2((x - .58f) * 1.06f, uv.y - .5f))) * 6f);
        float smear = sat((.58f - x) * 3f) * sat(x * 5f);
        float tailField = sat(((.17f + flow * .16f) * smear - y) * 7f);
        float shape = MathF.Max(headField, tailField);
        float body = shape * sat(.40f + flow * .55f + cells * .30f);
        float rim = sat(shape * 3.2f) * (1f - sat((shape - .28f) * 3.4f));
        float core = sat((headField - .48f) * 2.6f) * (.55f + cells * .6f);
        V3 color = lerp(C(8, 25, 8), C(66, 176, 54), body);
        color = lerp(color, C(213, 255, 133), sat(core + rim * .45f));
        float energy = body * .75f + core * 1.35f + rim * .5f;
        float alpha = sat(body + core + rim * .7f) * Opacity;
        return (color * (energy * alpha), alpha);
    }

    static (V3, float) ElandVenomImpact(V2 c, float drawSize, float Time, float Progress, float Opacity)
    {
        V2 uv = ElandPixelate(c, drawSize, drawSize);
        float r = length(uv - .5f);
        float cells = ElandPrimarySampler.R(uv * 2.2f + new V2(Time * .02f, -Time * .05f));
        float haze = ElandDetailSampler.R(uv * 2.8f - new V2(Time * .06f, Time * .04f));
        float grow = Progress * (2f - Progress);
        float splatR = .15f + grow * .28f + (cells - .5f) * .13f;
        float blob = sat((splatR - r) * 2.6f);
        float rimD = MathF.Abs(r - splatR);
        float rim = sat((.15f - rimD) * 5f) * (.45f + cells * .8f);
        float mist = sat((splatR + .22f - r) * 2f) * sat(haze * 1.5f - .45f);
        float outer = sat((.5f - r) * 3.6f); outer *= outer;
        float dens = sat(blob * (.45f + haze * .6f) + mist * .55f) * (1f - Progress * .30f) * outer;
        rim *= outer;
        V3 color = lerp(C(8, 25, 8), C(66, 176, 54), dens);
        color = lerp(color, C(213, 255, 133), sat(rim * .55f + sat(dens - .78f)));
        float alpha = sat(dens + rim * .7f) * Opacity;
        float energy = dens * .8f + rim * 1.15f;
        return (color * (energy * alpha), alpha);
    }

    static Panel[] Panels() => FocusPanels ?? AllPanels();

    // Set from Main via the FOCUS env var to render a single technique big.
    static Panel[] FocusPanels;

    static Panel[] AllPanels() => new[]
    {
        // EXACT sizes the call sites pass. DrawGroundWave body pass = 102x42, crest = 64x58;
        // DrawStandardCharge (empowered, full charge) = 128x96; DrawDominionEngulf at NPC.scale
        // 1.15 = 110x124. Aspect matters — a technique tuned at 1:1 falls apart at the real one.
        new Panel("Wave body P=.55", 151, 56, Blend.PremultipliedAlpha,
            c => DestinedDeathFlame(c, 3.7f, 0.55f, 0.9f, 0.9f, 1.7f)),
        new Panel("Wave crest P=.7", 94, 78, Blend.PremultipliedAlpha,
            c => DestinedDeathFlame(c, 3.7f, 0.7f, 1f, 1.15f, -0.8f)),
        new Panel("Standard charge P=1", 190, 130, Blend.PremultipliedAlpha,
            c => DestinedDeathFlame(c, 6.4f, 1f, 0.98f, 1.2f, 1.3f)),
        new Panel("Standard charge P=.05", 190, 130, Blend.PremultipliedAlpha,
            c => DestinedDeathFlame(c, 6.4f, 0.05f, 0.30f, 1.2f, 1.3f)),
        new Panel("Dominion engulf P=.5", 161, 161, Blend.PremultipliedAlpha,
            c => DestinedDeathFlame(c, 9.1f, 0.5f, 0.95f, 1.15f, 0f)),
        new Panel("Engulf side P=.62", 90, 202, Blend.PremultipliedAlpha,
            c => DestinedDeathFlame(c, 9.1f, 0.62f, 0.76f, 1.3f, 2.9f)),

        new Panel("Seal P=0.15", 190, 190, Blend.PremultipliedAlpha,
            c => DestinedDeathSeal(c, 4.2f, 0.15f, 0.92f, 1f)),
        new Panel("Seal P=0.60", 190, 190, Blend.PremultipliedAlpha,
            c => DestinedDeathSeal(c, 5.9f, 0.60f, 0.96f, 1f)),
        new Panel("Seal P=1.00", 190, 190, Blend.PremultipliedAlpha,
            c => DestinedDeathSeal(c, 7.4f, 1.00f, 1.00f, 1f)),

        new Panel("Blast P=0.2", 190, 190, Blend.PremultipliedAlpha,
            c => DestinedDeathBlast(c, 8.1f, 0.2f, 1f, 1f)),
        new Panel("Blast P=0.7", 190, 190, Blend.PremultipliedAlpha,
            c => DestinedDeathBlast(c, 8.4f, 0.7f, 0.85f, 1f)),

        // Stormbreaker lane is 620x46 active / 620x27 telegraph; halved here so it fits the sheet.
        new Panel("Bolt telegraph A=0", 310, 14, Blend.PremultipliedAlpha,
            c => LightningBolt(c, 2.2f, 0.6f, 0.9f, 0f, 3f)),
        new Panel("Bolt strike A=1", 310, 23, Blend.PremultipliedAlpha,
            c => LightningBolt(c, 2.9f, 1.0f, 1f, 1f, 3f)),
        new Panel("Bolt strike alt row", 310, 23, Blend.PremultipliedAlpha,
            c => LightningBolt(c, 2.9f, 1.0f, 1f, 1f, 8f)),

        new Panel("Herald P=0.25", 180, 180, Blend.PremultipliedAlpha,
            c => StormHerald(c, 3.3f, 0.25f, 0.9f)),
        new Panel("Herald P=0.75", 180, 180, Blend.PremultipliedAlpha,
            c => StormHerald(c, 6.6f, 0.75f, 0.95f)),
    };

    // ---------------------------------------------------------------------------------------------
    // Compositing + sheet layout.
    // ---------------------------------------------------------------------------------------------

    const int Zoom = 2;          // effects are small; nobody can judge a 74px puff at 1:1
    const int CellPad = 14;
    const int LabelH = 18;
    const int SheetTitleH = 22;

    static void Main()
    {
        LoadTextures();
        string previewName = Environment.GetEnvironmentVariable("PREVIEW_NAME") ?? "shader-preview";
        string safePreviewName = string.Concat(previewName.Select(c =>
            char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-')).Trim('-');
        if (string.IsNullOrWhiteSpace(safePreviewName)) safePreviewName = "shader-preview";
        if (Environment.GetEnvironmentVariable("FOCUS") == "eland")
        {
            FocusPanels = new[]
            {
                // Big nova uses its 400px damage radius plus the 1.3x visual envelope.
                new Panel("Nova telegraph P=.45", 1040, 1040, Blend.PremultipliedAlpha,
                    c => ElandToxicField(c, 1040f, 3.2f, .45f, 0f, .68f, 1f / 1.3f)),
                new Panel("Nova active P=.55", 1040, 1040, Blend.PremultipliedAlpha,
                    c => ElandToxicField(c, 1040f, 4.7f, .55f, 1f, .78f, 1f / 1.3f)),
                // Trail puffs deliberately reserve a larger, dimmer envelope for seamless overlap.
                new Panel("Trail smog P=.45", 166, 166, Blend.PremultipliedAlpha,
                    c => ElandToxicField(c, 166f, 4.1f, .45f, 1f, .56f, 1f / 1.8f)),
                new Panel("Burst aura P=.20", 38, 38, Blend.PremultipliedAlpha,
                    c => ElandPoisonBurstAura(c, 38f, 3.7f, .20f, .66f)),
                new Panel("Burst aura P=.70", 38, 38, Blend.PremultipliedAlpha,
                    c => ElandPoisonBurstAura(c, 38f, 3.7f, .70f, .66f)),
                new Panel("Burst aura P=.92", 38, 38, Blend.PremultipliedAlpha,
                    c => ElandPoisonBurstAura(c, 38f, 3.7f, .92f, .66f)),
                new Panel("Venom glob", 34, 27, Blend.PremultipliedAlpha,
                    c => ElandVenomGlob(c, 34f, 27f, 3.8f, .9f)),
                new Panel("Venom impact P=.45", 92, 92, Blend.PremultipliedAlpha,
                    c => ElandVenomImpact(c, 92f, 4.6f, .45f, .9f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "wall")
        {
            // The wall/shockwave sizes after the widening that the softer end-taper needs.
            FocusPanels = new[]
            {
                new Panel("Wave body P=.55", 152, 78, Blend.PremultipliedAlpha,
                    c => DestinedDeathFlame(c, 3.7f, 0.55f, 0.9f, 0.9f, 1.7f)),
                new Panel("Wave crest P=.7", 96, 96, Blend.PremultipliedAlpha,
                    c => DestinedDeathFlame(c, 4.9f, 0.7f, 1f, 1.15f, -0.8f)),
                new Panel("Charge P=.67", 190, 130, Blend.PremultipliedAlpha,
                    c => DestinedDeathFlame(c, 6.4f, 0.67f, 0.98f, 1.2f, 1.3f)),
                new Panel("Charge P=.05", 190, 130, Blend.PremultipliedAlpha,
                    c => DestinedDeathFlame(c, 6.4f, 0.05f, 0.4f, 1.2f, 1.3f)),
                new Panel("Engulf P=.5", 140, 150, Blend.PremultipliedAlpha,
                    c => DestinedDeathFlame(c, 9.1f, 0.5f, 0.95f, 1.15f, 0f)),
                new Panel("Engulf P=.95", 140, 150, Blend.PremultipliedAlpha,
                    c => DestinedDeathFlame(c, 12.3f, 0.95f, 0.95f, 1.15f, 0f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "bomb")
        {
            // A/B for RedKnightBurstKind.BombExplosionLayered. Top row = the old single quad,
            // bottom row = the three-shell layered composite, at matched Progress values across
            // the bomb's 44-tick life. Panel is the widest shell's footprint (182x168).
            FocusPanels = new[]
            {
                new Panel("OLD single P=.10", 182, 168, Blend.PremultipliedAlpha,
                    c => BombLayeredComposite(c, 5.2f, 0.10f, layered: false)),
                new Panel("OLD single P=.40", 182, 168, Blend.PremultipliedAlpha,
                    c => BombLayeredComposite(c, 5.6f, 0.40f, layered: false)),
                new Panel("OLD single P=.75", 182, 168, Blend.PremultipliedAlpha,
                    c => BombLayeredComposite(c, 6.1f, 0.75f, layered: false)),
                new Panel("NEW layered P=.10", 182, 168, Blend.PremultipliedAlpha,
                    c => BombLayeredComposite(c, 5.2f, 0.10f, layered: true)),
                new Panel("NEW layered P=.40", 182, 168, Blend.PremultipliedAlpha,
                    c => BombLayeredComposite(c, 5.6f, 0.40f, layered: true)),
                new Panel("NEW layered P=.75", 182, 168, Blend.PremultipliedAlpha,
                    c => BombLayeredComposite(c, 6.1f, 0.75f, layered: true)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "herald")
        {
            FocusPanels = new[]
            {
                new Panel("Herald P=0.20", 180, 180, Blend.PremultipliedAlpha,
                    c => StormHerald(c, 3.3f, 0.20f, 0.92f)),
                new Panel("Herald P=0.55", 180, 180, Blend.PremultipliedAlpha,
                    c => StormHerald(c, 5.1f, 0.55f, 0.95f)),
                new Panel("Herald P=0.85", 180, 180, Blend.PremultipliedAlpha,
                    c => StormHerald(c, 6.6f, 0.85f, 0.98f)),
                new Panel("Herald P=1.00", 180, 180, Blend.PremultipliedAlpha,
                    c => StormHerald(c, 8.0f, 1.00f, 0.98f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_pillar")
        {
            FocusPanels = new[]
            {
                new Panel("Telegraph P=0.25", 80, 520, Blend.PremultipliedAlpha,
                    c => GigasSunPillar(c, 2.4f, 0.25f, 0f, 0.70f)),
                new Panel("Telegraph P=0.70", 80, 520, Blend.PremultipliedAlpha,
                    c => GigasSunPillar(c, 3.6f, 0.70f, 0f, 0.70f)),
                new Panel("Strike P=1.00", 80, 520, Blend.PremultipliedAlpha,
                    c => GigasSunPillar(c, 4.8f, 1.00f, 1f, 0.92f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_sweep")
        {
            FocusPanels = new[]
            {
                new Panel("Telegraph P=0.25", 752, 72, Blend.PremultipliedAlpha,
                    c => GigasSweepBeam(c, 2.4f, 0.25f, 0f, 0.58f, 1f)),
                new Panel("Telegraph P=0.80", 752, 72, Blend.PremultipliedAlpha,
                    c => GigasSweepBeam(c, 3.6f, 0.80f, 0f, 0.58f, 1f)),
                new Panel("Strike P=1.00", 752, 72, Blend.PremultipliedAlpha,
                    c => GigasSweepBeam(c, 4.8f, 1.00f, 1f, 0.94f, 1f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_nova")
        {
            FocusPanels = new[]
            {
                new Panel("Smooth telegraph", 572, 572, Blend.PremultipliedAlpha, c => GigasNovaField(c, 3.7f, .80f, 0f, .58f, false)),
                new Panel("2px telegraph", 572, 572, Blend.PremultipliedAlpha, c => GigasNovaField(c, 3.7f, .80f, 0f, .58f)),
                new Panel("Smooth molten sun", 768, 768, Blend.PremultipliedAlpha, c => GigasNovaField(c, 4.9f, 1f, 1f, 1f, false)),
                new Panel("2px molten sun", 768, 768, Blend.PremultipliedAlpha, c => GigasNovaField(c, 4.9f, 1f, 1f, 1f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_light_hand")
        {
            FocusPanels = new[]
            {
                new Panel("Slabs emerge P=.25", 320, 180, Blend.PremultipliedAlpha, c => GigasLightHand(c, 2.4f, .25f, 0f)),
                new Panel("Slabs close P=.72", 320, 180, Blend.PremultipliedAlpha, c => GigasLightHand(c, 3.8f, .72f, 0f)),
                new Panel("Compression seam", 320, 180, Blend.PremultipliedAlpha, c => GigasLightHand(c, 4.9f, 1f, 1f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_light_hand_rich")
        {
            FocusPanels = new[]
            {
                new Panel("Slabs emerge P=.25", 320, 180, Blend.PremultipliedAlpha, c => GigasLightHandRich(c, 2.4f, .25f, 0f)),
                new Panel("Slabs close P=.72", 320, 180, Blend.PremultipliedAlpha, c => GigasLightHandRich(c, 3.8f, .72f, 0f)),
                new Panel("Compression seam", 320, 180, Blend.PremultipliedAlpha, c => GigasLightHandRich(c, 4.9f, 1f, 1f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_light_hand_luminous")
        {
            FocusPanels = new[]
            {
                new Panel("Approach — living light behind slabs", 320, 180, Blend.PremultipliedAlpha, c => GigasLightHandLuminous(c, 4.4f, .72f, 0f)),
                new Panel("Contact — compact nova burst", 320, 180, Blend.PremultipliedAlpha, c => GigasLightHandLuminous(c, 5.1f, 1f, 1f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_solar_boulder")
        {
            FocusPanels = new[]
            {
                new Panel("Early flight t=2.1", 88, 88, Blend.PremultipliedAlpha, c => GigasSolarBoulder(c, 2.1f, .72f)),
                new Panel("Flight motion t=3.4", 88, 88, Blend.PremultipliedAlpha, c => GigasSolarBoulder(c, 3.4f, .88f)),
                new Panel("Hot descent t=4.7", 88, 88, Blend.PremultipliedAlpha, c => GigasSolarBoulder(c, 4.7f, 1f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_halo_suns")
        {
            FocusPanels = new[]
            {
                new Panel("Orbit slot A", 56, 56, Blend.PremultipliedAlpha, c => GigasHaloSun(c, 2.2f, .08f, 0f)),
                new Panel("Orbit slot B", 56, 56, Blend.PremultipliedAlpha, c => GigasHaloSun(c, 3.7f, .41f, 0f)),
                new Panel("Launch flare", 56, 56, Blend.PremultipliedAlpha, c => GigasHaloSun(c, 4.9f, .73f, 1f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_consecrated_ground")
        {
            FocusPanels = new[]
            {
                new Panel("Unfiltered fresh", 52, 180, Blend.PremultipliedAlpha, c => GigasConsecratedGround(c, 2.4f, 1f, false)),
                new Panel("2px pixel fresh", 52, 180, Blend.PremultipliedAlpha, c => GigasConsecratedGround(c, 2.4f, 1f)),
                new Panel("2px pixel sustained", 52, 180, Blend.PremultipliedAlpha, c => GigasConsecratedGround(c, 4.1f, .62f)),
                new Panel("2px pixel embers", 52, 180, Blend.PremultipliedAlpha, c => GigasConsecratedGround(c, 5.8f, .18f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_heavenly_spears")
        {
            FocusPanels = new[]
            {
                new Panel("Harmless formation", 58, 26, Blend.PremultipliedAlpha, c => GigasHeavenlySpear(c, 2.4f, .38f, 0f)),
                new Panel("First wave lance", 72, 28, Blend.PremultipliedAlpha, c => GigasHeavenlySpear(c, 3.8f, 1f, 1f)),
                new Panel("Final wave wake", 160, 38, Blend.PremultipliedAlpha, c => GigasHeavenlySpear(c, 5.2f, 1f, 1f)),
            };
        }
        if (Environment.GetEnvironmentVariable("FOCUS") == "gigas_heavenly_spears_layered")
        {
            FocusPanels = new[]
            {
                new Panel("Formation detail", 58, 26, Blend.PremultipliedAlpha, c => GigasHeavenlySpearLayered(c, 2.4f, .38f, 0f)),
                new Panel("First wave layered", 72, 28, Blend.PremultipliedAlpha, c => GigasHeavenlySpearLayered(c, 3.8f, 1f, 1f)),
                new Panel("Final wake layered", 160, 38, Blend.PremultipliedAlpha, c => GigasHeavenlySpearLayered(c, 5.2f, 1f, 1f)),
            };
        }
        var panels = Panels();

        // Every panel is drawn over BOTH a bright daytime sky and a dark cave. This is the single
        // most valuable thing the harness does: additive blending cannot produce a saturated colour
        // over a bright background — anything vivid enough to see against the sky clips to white —
        // while premultiplied alpha can look great against sky and muddy in a cave. You need both.
        int maxW = 0, maxH = 0;
        foreach (var p in panels) { maxW = Math.Max(maxW, p.W * Zoom); maxH = Math.Max(maxH, p.H * Zoom); }
        int half = maxW + CellPad * 2;
        int cellW = half * 2, cellH = maxH + CellPad * 2 + LabelH;

        int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(panels.Length)));
        int rows = (panels.Length + cols - 1) / cols;

        using var sheet = new Bitmap(cols * cellW, rows * cellH + SheetTitleH);
        using var g = Graphics.FromImage(sheet);
        using var font = new Font("Consolas", 10);
        g.Clear(Color.FromArgb(24, 24, 30));
        g.DrawString($"{previewName} — sky | cave — {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            font, Brushes.Gold, 4, 3);

        for (int i = 0; i < panels.Length; i++)
        {
            var panel = panels[i];
            int ox = (i % cols) * cellW, oy = SheetTitleH + (i / cols) * cellH + LabelH;

            for (int side = 0; side < 2; side++)
            {
                bool cave = side == 1;
                int bx = ox + side * half;
                // Background.
                for (int y = 0; y < cellH - LabelH; y++)
                {
                    Color bg = cave
                        ? Color.FromArgb(255, 26, 22, 30)
                        : Color.FromArgb(255, 96 + 60 * y / (cellH - LabelH),
                                              140 + 50 * y / (cellH - LabelH),
                                              200 + 35 * y / (cellH - LabelH));
                    for (int x = 0; x < half; x++) sheet.SetPixel(bx + x, oy + y, bg);
                }

                int dw = panel.W * Zoom, dh = panel.H * Zoom;
                int px = bx + (half - dw) / 2, py = oy + (cellH - LabelH - dh) / 2;
                for (int y = 0; y < dh; y++)
                {
                    for (int x = 0; x < dw; x++)
                    {
                        var (rgb, a) = panel.Shade(new V2((x + 0.5f) / dw, (y + 0.5f) / dh));
                        var bg = sheet.GetPixel(px + x, py + y);
                        float br = bg.R / 255f, bgc = bg.G / 255f, bb = bg.B / 255f;
                        // XNA BlendState.Additive     = SourceAlpha / One  -> dst + rgb * a
                        // XNA BlendState.AlphaBlend   = One / InvSourceAlpha (PREMULTIPLIED)
                        //                                                  -> rgb + dst * (1 - a)
                        float rr, gg, bbv;
                        if (panel.Mode == Blend.Additive)
                        { rr = br + rgb.x * a; gg = bgc + rgb.y * a; bbv = bb + rgb.z * a; }
                        else
                        { rr = rgb.x + br * (1 - a); gg = rgb.y + bgc * (1 - a); bbv = rgb.z + bb * (1 - a); }
                        sheet.SetPixel(px + x, py + y, Color.FromArgb(255,
                            (int)(sat(rr) * 255), (int)(sat(gg) * 255), (int)(sat(bbv) * 255)));
                    }
                }
            }

            g.FillRectangle(Brushes.Black, ox, oy - LabelH, cellW, LabelH);
            g.DrawString($"{panel.Name}  [{panel.W}x{panel.H}, {panel.Mode}]  sky | cave",
                font, Brushes.Yellow, ox + 3, oy - LabelH + 2);
            g.DrawRectangle(Pens.DimGray, ox, oy - LabelH, cellW - 1, cellH - 1);
        }

        string outputName = $"{safePreviewName}-{DateTime.Now:yyyyMMdd-HHmmss}.png";
        string outputDirectory = Environment.GetEnvironmentVariable("PREVIEW_OUTPUT_DIR");
        string outputPath = string.IsNullOrWhiteSpace(outputDirectory) ? outputName : Path.Combine(outputDirectory, outputName);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        sheet.Save(outputPath, ImageFormat.Png);
        Console.WriteLine($"wrote {outputPath} ({panels.Length} panels)");
    }
}

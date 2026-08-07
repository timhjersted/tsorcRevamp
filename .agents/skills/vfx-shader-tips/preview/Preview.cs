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

static class Preview
{
    // ---------------------------------------------------------------------------------------------
    // Harness. You should not need to touch anything above "YOUR SHADERS".
    // ---------------------------------------------------------------------------------------------

    const string NoiseRoot = @"..\..\..\..\Textures\Noise\";

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

        public Tex(string name)
        {
            using var bmp = new Bitmap(NoiseRoot + name + ".png");
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
    static Tex MacroSampler, DetailSampler;

    static void LoadTextures()
    {
        MacroSampler = new Tex("T_Noise_6Yu1");
        DetailSampler = new Tex("Turbulence_07-512x512");
        CrimsonFlowSampler = new Tex("Turbulence_05-512x512");
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

    static void Main()
    {
        LoadTextures();
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

        using var sheet = new Bitmap(cols * cellW, rows * cellH);
        using var g = Graphics.FromImage(sheet);
        using var font = new Font("Consolas", 10);
        g.Clear(Color.FromArgb(24, 24, 30));

        for (int i = 0; i < panels.Length; i++)
        {
            var panel = panels[i];
            int ox = (i % cols) * cellW, oy = (i / cols) * cellH + LabelH;

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

        sheet.Save("preview.png", ImageFormat.Png);
        Console.WriteLine($"wrote preview.png ({panels.Length} panels)");
    }
}

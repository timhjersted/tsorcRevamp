// Small side-view flame shared by Minotaur bolts, ground waves, tells and floor fire.
sampler NoiseSampler : register(s0);
float Time;
float Opacity;
float Seed;
float4 PixelGrid;

float4 FlamePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (floor(coords * PixelGrid.xy) + 0.5) * PixelGrid.zw;

    // Opposing anisotropic upward-scrolling noise (+Time in UV.y moves features toward y=0).
    // High X frequency carves multiple independent flame tongues instead of one central triangle.
    float macro = tex2D(NoiseSampler, uv * float2(3.1, 1.15) + float2(Seed + Time * 0.22, Time * 1.05)).r;
    float tongues = tex2D(NoiseSampler, uv * float2(6.2, 1.85) + float2(-Seed - Time * 0.30, Time * 1.75)).r;

    float height = saturate((0.92 - uv.y) * 1.18);
    float sway = (macro - 0.5) * (0.12 + height * 0.34) + (tongues - 0.5) * height * 0.16;
    float across = abs(uv.x - 0.5 + sway);

    // Rounded ember base (curves up at the sides instead of a flat horizontal ruler line) + undulating top.
    float baseRound = saturate((0.92 - uv.y - across * across * 0.95) * 10.0);
    float topGuard = saturate((uv.y - 0.06) * 6.0);
    float sideGuard = saturate((0.46 - abs(uv.x - 0.5)) * 10.0);

    // Wide organic belly that tapers smoothly into curling multi-tongued tips (never a blunt block or rigid triangle).
    float width = (0.41 - height * height * 0.26) * (0.46 + macro * 0.80);
    float envelope = saturate((width - across) * 5.2);
    float rawFlame = envelope * baseRound * topGuard * sideGuard * (macro * 0.80 + tongues * 0.95) - height * 0.62;
    float body = saturate(rawFlame * 2.35);

    float heat = body * body;
    float core = heat * heat * saturate(1.18 - height * 1.75);

    // Vivid fiery gradient: rich red-orange licking tips -> blazing orange body -> bright gold heart near the base.
    float3 color = float3(0.95, 0.14, 0.015) * body
        + float3(0.45, 0.40, 0.015) * heat
        + float3(0.32, 0.34, 0.12) * core;
    float alpha = saturate(body * 2.0) * Opacity;
    return float4(color * Opacity * vertexColor.rgb, alpha * vertexColor.a);
}

technique MinotaurFire
{
    pass FlamePass { PixelShader = compile ps_2_0 FlamePixel(); }
}

// Flight uses a rounded leading cap instead of rotating the floor flame's flat base.
// The positive-Y end leads, and body noise flows toward the negative-Y tail.
float4 BoltPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (floor(coords * PixelGrid.xy) + 0.5) * PixelGrid.zw;
    float noise = tex2D(NoiseSampler, uv * float2(1.8, 0.65) + float2(Seed, Time * 0.85)).r;
    float detail = tex2D(NoiseSampler, uv * float2(3.7, 1.1) + float2(-Seed, Time * 1.3)).r;
    float across = abs(uv.x - 0.5);
    float tail = saturate((uv.y - 0.10) * 1.5);
    float width = (0.08 + tail * 0.26) * (0.80 + noise * 0.20);
    float edge = saturate((width - across) * 18.0);
    // Rounded parabolic nose: outermost point is y=.91, and sides curve back into the body.
    // tail=0 below y=.10; width<=.34<.5; cap=0 above y=.91. All four quad edges stay empty.
    float cap = saturate((0.91 - uv.y - across * across * 2.2) * 14.0);
    float body = edge * cap * tail * saturate(noise * 1.1 + detail * 0.55);
    float heat = body * body;
    float core = heat * saturate(1.0 - abs(uv.y - 0.68) * 5.0)
        * saturate((0.18 - across) * 8.0);
    float3 color = float3(0.78, 0.045, 0.003) * body
        + float3(0.30, 0.32, 0.012) * heat
        + float3(0.35, 0.32, 0.10) * core;
    return float4(color * Opacity * vertexColor.rgb,
        saturate(body * 1.5) * Opacity * vertexColor.a);
}

technique MinotaurFirebolt
{
    pass BoltPass { PixelShader = compile ps_2_0 BoltPixel(); }
}

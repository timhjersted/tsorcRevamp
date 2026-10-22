// Small side-view flame shared by Minotaur bolts, ground waves, tells and floor fire.
sampler NoiseSampler : register(s0);
float Time;
float Opacity;
float Seed;
float4 PixelGrid;

float4 FlamePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (floor(coords * PixelGrid.xy) + 0.5) * PixelGrid.zw;
    float noise = tex2D(NoiseSampler, uv * float2(1.8, 0.65) + float2(Seed, -Time * 0.85)).r;
    float detail = tex2D(NoiseSampler, uv * float2(3.7, 1.1) + float2(-Seed, -Time * 1.3)).r;
    float height = saturate((0.94 - uv.y) * 1.2);
    float width = (0.40 - height * 0.34) * (0.60 + noise * 0.45);
    float across = abs(uv.x - 0.5 + (noise - 0.5) * height * 0.15);
    // width <= .42 and lateral displacement <= .075: side envelope vanishes before x=0/1.
    // Vertical cutoff is noise-independent, zero at y<=.08 and y>=.94.
    float edge = saturate((width - across) * 14.0);
    float ends = saturate((uv.y - 0.08) * 5.0) * saturate((0.94 - uv.y) * 18.0);
    float body = edge * ends * saturate(noise * 1.1 + detail * 0.55 - height * 0.75);
    float heat = body * body;
    float core = heat * heat * saturate(1.0 - height * 2.5);
    float3 color = float3(0.78, 0.045, 0.003) * body
        + float3(0.30, 0.32, 0.012) * heat
        + float3(0.35, 0.32, 0.10) * core;
    float alpha = saturate(body * 1.5) * Opacity;
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

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float Direction;
float2 DrawSize;
float2 PrimaryTextureSize;

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 CrescentPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv - 0.5;
    float noise = tex2D(DetailSampler, uv * 3.2 + float2(Time * 0.1, -Time * 0.07)).r;
    float outer = saturate((0.47 - length(p)) * 18.0);
    float cutout = saturate((length(p + float2(0.18, 0.0)) - 0.29) * 16.0);
    float crescent = outer * cutout;
    float silver = saturate((crescent - 0.58) * 2.38);
    float fringe = crescent * (0.66 + noise * 0.34);
    float3 color = lerp(DarkColor, MidColor, fringe);
    color = lerp(color, CoreColor, silver);
    return float4(sampleColor.rgb * color * (fringe * 0.72 + silver * 1.25),
        sampleColor.a * crescent * Opacity);
}

float4 OrbPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv - 0.5;
    float radius = length(p) * 2.0;
    float spiral = tex2D(PrimarySampler, uv + float2(Time * 0.035 * Direction, -Time * 0.025)).r;
    float noise = tex2D(DetailSampler, uv * 3.8 + float2(-Time * 0.12, Time * 0.08)).r;
    float orb = saturate((1.0 - radius) * 8.0);
    float core = saturate((0.48 + Active * 0.16 - radius) * 8.0);
    float fringe = orb * saturate(spiral * 0.68 + noise * 0.5 - 0.28);
    float danger = Active;
    float3 dangerColor = lerp(MidColor, float3(0.95, 0.24, 0.68), danger);
    float3 color = lerp(DarkColor, dangerColor, fringe);
    color = lerp(color, CoreColor, core);
    return float4(sampleColor.rgb * color * (orb * 0.55 + fringe * 0.65 + core * 1.4),
        sampleColor.a * saturate(orb * 0.72 + fringe + core) * Opacity);
}

float4 TrailPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float streak = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.0, 3.5) + float2(-Time * 0.18, Time * 0.04)).r;
    float taper = saturate(uv.x * 4.5) * saturate((1.0 - uv.x) * 2.0);
    float body = saturate(streak * 0.75 + noise * 0.35 - 0.27) * taper;
    float core = body * body * (0.45 + Active * 0.55);
    float3 stateColor = lerp(MidColor, float3(0.95, 0.24, 0.68), Active);
    float3 color = lerp(DarkColor, stateColor, body);
    color = lerp(color, CoreColor, core);
    return float4(sampleColor.rgb * color * (body * 0.68 + core * 1.2),
        sampleColor.a * body * Opacity);
}

float4 TurnFlashPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float flare = tex2D(PrimarySampler, uv).r;
    float pulse = flare * (1.0 - Progress * 0.65);
    float3 color = lerp(MidColor, CoreColor, pulse);
    return float4(sampleColor.rgb * color * pulse * 1.3,
        sampleColor.a * pulse * Opacity);
}

technique ArtoriasCrescent
{
    pass CrescentPass { PixelShader = compile ps_2_0 CrescentPixel(); }
}
technique ArtoriasOrb
{
    pass OrbPass { PixelShader = compile ps_2_0 OrbPixel(); }
}
technique ArtoriasProjectileTrail
{
    pass TrailPass { PixelShader = compile ps_2_0 TrailPixel(); }
}
technique ArtoriasTurnFlash
{
    pass TurnFlashPass { PixelShader = compile ps_2_0 TurnFlashPixel(); }
}

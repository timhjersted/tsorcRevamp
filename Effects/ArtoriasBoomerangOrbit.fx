// ArtoriasBoomerangOrbit.fx
// Counter-rotating abyss wisps around the hostile crescent's crisp core.

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Active;
float Direction;

float2 RotateUV(float2 p, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    return float2(p.x * c - p.y * s, p.x * s + p.y * c);
}

float4 BoomerangOrbitPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 p = coords - 0.5;
    float radius = length(p);
    float spin = Time * (0.82 + Active * 0.24) * Direction;
    float2 uvA = RotateUV(p, spin) + 0.5;
    float2 uvB = RotateUV(p, -spin * 0.63) + 0.5;

    float spiralA = tex2D(PrimarySampler, uvA).r;
    float spiralB = tex2D(PrimarySampler, uvB * 1.23 - 0.115).r;
    float noise = tex2D(DetailSampler,
        coords * 3.2 + float2(Time * 0.09 * Direction, -Time * 0.12)).r;

    float outer = saturate((0.51 - radius) * 13.0);
    float hollow = saturate((radius - 0.10) * 13.0);
    float brokenSpiral = saturate(spiralA * 0.70 + spiralB * 0.45 + noise * 0.35 - 0.43);
    float wisps = brokenSpiral * outer * hollow;
    float hot = saturate((wisps - 0.42) * 2.25);

    float3 returnColor = lerp(MidColor, float3(0.92, 0.18, 0.72), Active * 0.72);
    float3 color = lerp(DarkColor, returnColor, wisps);
    color = lerp(color, CoreColor, hot * 0.58);
    return float4(vertexColor.rgb * color * (wisps * 0.90 + hot * 0.70),
        vertexColor.a * wisps * Opacity);
}

technique ArtoriasBoomerangOrbit
{
    pass OrbitPass { PixelShader = compile ps_2_0 BoomerangOrbitPixel(); }
}

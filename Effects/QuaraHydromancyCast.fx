// QuaraHydromancyCast.fx
// Animated staff cast sigils for QuaraHydromancer in Reach ps_2_0

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float4 uSourceRect;

float4 BarrageCast(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edgeMask = saturate(pow(1.0 - saturate(r), 1.8));

    float2 rotUV = float2(p.x * cos(Time * 2.5) - p.y * sin(Time * 2.5), p.x * sin(Time * 2.5) + p.y * cos(Time * 2.5)) * 0.5 + 0.5;
    float n1 = tex2D(DetailSampler, rotUV * 1.8).r;
    float n2 = tex2D(DetailSampler, rotUV * 3.2 + float2(-Time * 0.15, Time * 0.12)).r;

    float ring1 = saturate(1.0 - abs(r - 0.70) * 5.0);
    float ring2 = saturate(1.0 - abs(r - 0.40) * 6.0);
    float swirl = saturate(n1 * 1.2 + n2 * 0.8 - 0.4) * (ring1 + ring2);

    float3 color = lerp(DarkColor, MidColor, swirl);
    color = lerp(color, CoreColor, ring1 * n1 + ring2 * 0.5);

    float alpha = saturate(swirl * 1.4 + ring1 * 0.8) * edgeMask * Opacity;
    return float4(v.rgb * color * (1.0 + ring1 * 0.6), alpha) * v.a;
}

float4 CrestCast(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edgeMask = saturate(pow(1.0 - saturate(r), 1.8));

    float n = tex2D(DetailSampler, c * 2.2 + float2(-Time * 0.2, Time * 0.08)).r;
    float crescent = saturate(1.0 - abs(r - 0.65) * 4.5);
    float arc = saturate((p.y + 0.6) * 1.5) * crescent;
    float waveLine = saturate(1.0 - abs(p.y - (n - 0.5) * 0.25) * 6.0);

    float body = arc * 0.8 + waveLine * 0.7;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, waveLine * 1.2);

    float alpha = saturate(body) * edgeMask * Opacity;
    return float4(v.rgb * color * (1.0 + waveLine * 0.8), alpha) * v.a;
}

float4 BubbleCast(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edgeMask = saturate(pow(1.0 - saturate(r), 1.8));

    float sphereRadius = lerp(0.3, 0.75, Progress);
    float sphere = saturate(1.0 - abs(r - sphereRadius) * 6.0);

    float n1 = tex2D(DetailSampler, c * 2.8 + float2(Time * 0.15, -Time * 0.12)).r;
    float n2 = tex2D(DetailSampler, c * 4.5 + float2(-Time * 0.2, Time * 0.18)).r;
    float caustics = saturate(n1 * 1.3 + n2 * 0.7 - 0.5);

    float body = sphere + caustics * saturate(1.0 - r);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, sphere + caustics * 0.6);

    float alpha = saturate(body * 1.2) * edgeMask * Opacity;
    return float4(v.rgb * color * (1.0 + sphere * 0.8), alpha) * v.a;
}

float4 InkCast(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edgeMask = saturate(pow(1.0 - saturate(r), 1.8));

    float ring = saturate(1.0 - abs(r - 0.68) * 5.0);
    float inkNoise = tex2D(DetailSampler, c * 3.2 + float2(-Time * 0.10, Time * 0.14)).r;
    float blot = saturate(1.0 - r * 1.2) * saturate(inkNoise * 2.0 - 0.5);

    float body = ring * 0.7 + blot * 1.3;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, blot * (1.0 - r));

    float alpha = saturate(body) * edgeMask * Opacity;
    return float4(v.rgb * color * 1.2, alpha) * v.a;
}

float4 RushCast(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edgeMask = saturate(pow(1.0 - saturate(r), 1.8));

    float stream = saturate(1.0 - abs(p.y) * 4.0) * saturate(1.0 - abs(p.x) * 1.2);
    float n = tex2D(DetailSampler, c * 3.5 + float2(-Time * 0.4 * Direction, 0.0)).r;
    float body = stream * (0.6 + n * 0.6);

    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, stream * n * 1.2);

    float alpha = saturate(body * 1.3) * edgeMask * Opacity;
    return float4(v.rgb * color * 1.3, alpha) * v.a;
}

technique QuaraBarrageCast { pass P { PixelShader = compile ps_2_0 BarrageCast(); } }
technique QuaraCrestCast { pass P { PixelShader = compile ps_2_0 CrestCast(); } }
technique QuaraBubbleCast { pass P { PixelShader = compile ps_2_0 BubbleCast(); } }
technique QuaraInkCast { pass P { PixelShader = compile ps_2_0 InkCast(); } }
technique QuaraRushCast { pass P { PixelShader = compile ps_2_0 RushCast(); } }

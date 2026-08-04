// RedKnightCrimsonVFX.fx
// Side-view, noise-shaped combat materials for Red Knight and Great Red Knight.
// The primary sprite is a one-pixel white quad; both texture inputs are modulation data.

sampler BaseSampler : register(s0);
sampler FlowSampler : register(s1);
sampler DetailSampler : register(s2);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Intensity;
float Direction;
float TailStrength;
float2 DrawSize;

float4 SpearWakePixel(float4 vertexColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float x = uv.x;
    float y = abs(uv.y - 0.5) * 2.0;
    float broad = tex2D(FlowSampler,
        float2(x * 2.7 - Time * 0.82, uv.y * 1.9 + Time * 0.13)).r;
    float grit = tex2D(DetailSampler,
        float2(x * 5.6 - Time * 1.28, uv.y * 3.4 - Time * 0.09)).r;

    float taper = saturate(x * 5.5) * saturate((1.0 - x) * 5.5);
    float halfWidth = (0.20 + broad * 0.10) * taper;
    float body = saturate((halfWidth - y) * 7.5)
        * saturate(0.42 + broad * 0.52 + grit * 0.20);
    float filament = saturate((0.10 * taper - y) * 11.0) * taper;

    float3 color = lerp(DarkColor, MidColor, saturate(body * 0.72 + broad * 0.20));
    color = lerp(color, CoreColor, filament);
    float energy = body * (0.56 + broad * 0.34) + filament * 0.82;
    float alpha = saturate(body * 0.60 + filament * 0.66) * Opacity;
    return float4(vertexColor.rgb * color * energy, vertexColor.a * alpha);
}

float4 CrimsonFlamePixel(float4 vertexColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float mask = tex2D(BaseSampler, uv).a;
    float broad = tex2D(FlowSampler,
        uv * float2(2.35, 3.15) + float2(Time * 0.16 * Direction, -Time * 0.48)).r;
    float grit = tex2D(DetailSampler,
        uv * float2(5.4, 4.7) + float2(-Time * 0.29 * Direction, -Time * 0.17)).r;

    float lowerHeat = saturate((uv.y - 0.18) * 1.38);
    float erosionThreshold = 0.09 + (1.0 - broad) * 0.17 + (grit - 0.5) * 0.10;
    float body = saturate((mask - erosionThreshold) * 5.2);
    float brokenEdge = saturate((mask - 0.025) * 7.5) * saturate(broad * 1.38 + grit * 0.28 - 0.34);
    float core = saturate((mask - 0.46) * 3.4) * lowerHeat
        * saturate(broad * 1.25 + Progress * 0.25 - 0.28);
    float darkLobe = body * saturate(1.08 - broad * 0.86);

    float3 color = DarkColor * darkLobe;
    color += MidColor * (body * (0.42 + broad * 0.72) + brokenEdge * 0.26);
    color += CoreColor * core * 0.88;
    float energy = body * (0.50 + Intensity * 0.34) + brokenEdge * 0.30 + core * 0.82;
    float alpha = saturate(body * 0.76 + brokenEdge * 0.22 + core * 0.36) * Opacity;
    return float4(vertexColor.rgb * color * energy, vertexColor.a * alpha);
}

float4 GroundMiasmaPixel(float4 vertexColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float x = Direction < 0.0 ? 1.0 - uv.x : uv.x;
    float rise = 1.0 - uv.y;
    float broad = tex2D(FlowSampler,
        float2(x * 2.35 - Time * (0.34 + Intensity * 0.28), rise * 2.8 - Time * 0.44)).r;

    float longitudinal = saturate(x * 9.0) * saturate((1.0 - x) * 5.8);
    float surge = Progress * 0.76 + 0.24;
    float flameHeight = (0.13 + broad * (0.30 + Intensity * 0.34)
        ) * longitudinal * surge;
    float body = saturate((flameHeight - rise) * 11.0) * longitudinal;
    body *= saturate(0.42 + broad * 0.78);

    float baseLine = saturate((0.11 - rise) * 9.0) * longitudinal;
    float tongueCore = saturate((flameHeight * 0.44 - rise) * 13.0)
        * longitudinal * saturate(broad * 1.28 - 0.30);
    float fringe = saturate((flameHeight + 0.07 - rise) * 8.0)
        * longitudinal * saturate(broad * 1.55 - 0.68);

    float3 color = lerp(DarkColor, MidColor, saturate(body * 0.76 + broad * 0.20));
    color = lerp(color, CoreColor, saturate(baseLine * 0.46 + tongueCore * 0.72));
    float energy = body * (0.58 + Intensity * 0.34) + baseLine * 0.72 + tongueCore * 0.70 + fringe * 0.22;
    float alpha = saturate(body * 0.76 + baseLine * 0.54 + tongueCore * 0.58 + fringe * 0.20) * Opacity;
    return float4(vertexColor.rgb * color * energy, vertexColor.a * alpha);
}

float4 PoisonOrbPixel(float4 vertexColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float headX = 0.5 + TailStrength * 0.22;
    float2 headPoint = float2((uv.x - headX) / 0.285, (uv.y - 0.5) / 0.34);
    float broad = tex2D(FlowSampler,
        uv * float2(2.7, 2.35) + float2(-Time * 0.31, Time * 0.19)).r;
    float grit = tex2D(DetailSampler,
        uv * float2(5.1, 4.4) + float2(Time * 0.23, -Time * 0.14)).r;

    float orbDistance = length(headPoint) + (broad - 0.5) * 0.21;
    float orb = saturate((1.0 - orbDistance) * 5.6);
    float inner = saturate((0.54 - orbDistance) * 4.2);
    float membrane = saturate(1.0 - abs(orbDistance - 0.78) * 6.5) * orb * grit;

    float tailX = saturate(uv.x / headX);
    float tailCenter = 0.5 + (broad - 0.5) * 0.20 * (1.0 - tailX);
    float tailWidth = 0.025 + tailX * 0.15;
    float tail = saturate((tailWidth - abs(uv.y - tailCenter)) * 16.0)
        * saturate(uv.x * 7.0) * saturate((headX - uv.x) * 9.0)
        * saturate(broad + grit - 0.72) * TailStrength;

    float3 color = lerp(DarkColor, MidColor, saturate(orb + tail * 0.45));
    color = lerp(color, CoreColor, saturate(inner * 0.72 + membrane * 0.22));
    float energy = orb * (0.58 + broad * 0.44) + inner * 0.82 + membrane * 0.34 + tail * 0.60;
    float alpha = saturate(orb * 0.88 + inner * 0.42 + membrane * 0.24 + tail * 0.60) * Opacity;
    return float4(vertexColor.rgb * color * energy, vertexColor.a * alpha);
}

float4 BombBlastPixel(float4 vertexColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = (uv - 0.5) * 2.0;
    float broad = tex2D(FlowSampler,
        uv * 2.35 + float2(-Time * 0.22, Time * 0.17)).r;

    float eased = Progress * (2.0 - Progress);
    float radius = lerp(0.12, 0.92, eased);
    float distortedDistance = length(p) + (broad - 0.5) * 0.19;
    float body = saturate((radius - distortedDistance) * 5.9);
    float rollingEdge = saturate(1.0 - abs(distortedDistance - radius + 0.05) * 7.0)
        * saturate(broad * 1.2 - 0.2);
    float core = saturate((radius * 0.46 - distortedDistance) * 6.2) * (1.0 - Progress * 0.55);

    float3 color = lerp(DarkColor, MidColor, saturate(body * 0.68 + rollingEdge * 0.54));
    color = lerp(color, CoreColor, core);
    float energy = body * (0.52 + broad * 0.48) + rollingEdge * 0.86 + core * 1.18;
    float alpha = saturate(body * 0.70 + rollingEdge * 0.82 + core) * Opacity * (1.0 - Progress * 0.78);
    return float4(vertexColor.rgb * color * energy, vertexColor.a * alpha);
}

float4 UltrakillGatherPixel(float4 vertexColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = (uv - 0.5) * 2.0;
    float radius = length(p);
    float broad = tex2D(FlowSampler,
        uv * 2.45 + float2(-Time * 0.31, Time * 0.22)).r;
    float grit = tex2D(DetailSampler,
        uv * 5.3 + float2(Time * 0.24, -Time * 0.17)).r;

    float collapse = Progress * Progress * (3.0 - 2.0 * Progress);
    float limit = lerp(1.04, 0.27, collapse);
    float erosion = (broad - 0.5) * 0.22 + (grit - 0.5) * 0.08;
    float outerFade = saturate((1.08 - radius) * 8.0);
    float body = saturate((limit + erosion - radius) * 7.2) * outerFade;
    float front = saturate(1.0 - abs(radius - limit - erosion * 0.35) * 9.0)
        * saturate(broad * 1.35 + grit * 0.35 - 0.34) * outerFade;
    float veins = saturate((broad * 0.72 + grit * 0.58 - 0.70) * 4.2) * body;
    float coreLimit = lerp(0.07, 0.23, collapse);
    float core = saturate((coreLimit - radius + broad * 0.025) * 10.0);

    float3 color = lerp(DarkColor, MidColor, saturate(body * 0.68 + veins * 0.55));
    color = lerp(color, CoreColor, saturate(front * 0.42 + core * 0.92));
    float energy = body * (0.48 + broad * 0.42) + veins * 0.65 + front * 0.72 + core * 1.22;
    float alpha = saturate(body * 0.66 + veins * 0.42 + front * 0.62 + core) * Opacity;
    return float4(vertexColor.rgb * color * energy, vertexColor.a * alpha);
}

technique RedKnightSpearWake
{
    pass WakePass { PixelShader = compile ps_2_0 SpearWakePixel(); }
}

technique RedKnightCrimsonFlame
{
    pass FlamePass { PixelShader = compile ps_2_0 CrimsonFlamePixel(); }
}

technique RedKnightGroundMiasma
{
    pass GroundPass { PixelShader = compile ps_2_0 GroundMiasmaPixel(); }
}


technique RedKnightUltrakillGather
{
    pass GatherPass { PixelShader = compile ps_2_0 UltrakillGatherPixel(); }
}
technique RedKnightPoisonOrb
{
    pass PoisonPass { PixelShader = compile ps_2_0 PoisonOrbPixel(); }
}

technique RedKnightBombBlast
{
    pass BombPass { PixelShader = compile ps_2_0 BombBlastPixel(); }
}

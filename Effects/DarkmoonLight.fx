// Darkmoon moonlight: the Vigil moons' aim rays (telegraph) and the light shards they fire.
// Drawn additively on one quad each (DarkmoonVisuals.DrawLight). uv.x runs along the quad (0 = origin / tail,
// 1 = far end / head), uv.y across it. Returns float4(rgb * a, a); with additive blend only rgb matters.
// s0 = T_PerlinNoise_Tiled (the quad's own texture, sampled scrolled for flowing light).
#include "PixelShaderCommon.fxh"

sampler NoiseSampler : register(s0);

float Time;
float Opacity;      // overall fade 0..1 (pulse / lock brightness folded in by the caller)
float Aspect;       // quad length / width, so the shard's head flare stays round
float FlowScale;    // noise repeats along the quad (length / 96px for the ray)
float4 PixelGrid;   // xy = 2px block count across the quad, zw = reciprocal (computed in C#)

static const float3 MoonBlue = float3(0.36, 0.62, 1.0);
static const float3 PaleCyan = float3(0.72, 0.90, 1.0);

// Aim ray: soft quadratic falloff across the width with a narrow white-hot core, flowing shimmer travelling outward
// along the line, a quick fade-in over the first 8%, full strength to 38% and a linear fade to nothing at the far end.
float4 Ray(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float across = abs(uv.y - 0.5) * 2.0;

    float glow = 1.0 - across;
    glow = glow * glow;
    float core = saturate(1.0 - across * 5.0);
    core = core * core;

    float flow = tex2D(NoiseSampler, float2(uv.x * FlowScale - Time * 1.6, uv.y * 0.5)).r;
    float shimmer = 0.6 + flow * 0.8;

    float start = saturate(uv.x * 12.0);
    float end = saturate((1.0 - uv.x) * 1.6);

    float light = (glow * 1.1 * shimmer + core * 1.8) * start * end * Opacity;
    float3 color = lerp(MoonBlue, float3(1.0, 1.0, 1.0), saturate(core * 1.4));
    return float4(color * light, light);
}

// Light shard: a spindle that swells from the tail to its widest at 82% and pinches to a point at the head.
// Inside it: a soft halo (out to ~1.7x the spindle), the body, a white core line and noise streaks racing toward
// the tail. A round flare sits on the head point. The tail end is dimmer, so it reads as a streak of light in flight.
float4 Shard(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float side = (uv.y - 0.5) * 2.0;
    float across = abs(side);

    float rise = saturate(uv.x / 0.82);
    float pinch = saturate((1.0 - uv.x) / 0.18);
    float width = sqrt(rise * pinch) * 0.55;
    float inside = across / max(width, 0.02);

    float halo = saturate(1.0 - inside * 0.6);
    halo = halo * halo;
    float body = saturate(1.0 - inside);
    body = body * body;
    float coreLine = saturate(1.0 - inside * 3.0);

    float streak = tex2D(NoiseSampler, float2(uv.x * 2.5 + Time * 3.0, uv.y * 0.8)).r;
    float tail = 0.3 + 0.7 * uv.x;

    float2 fromHead = float2((uv.x - 0.82) * Aspect, side) * 0.5;
    float flare = saturate(1.0 - length(fromHead) * 2.2);
    flare = flare * flare;

    float light = ((halo * 0.45 + body * (0.35 + streak * 0.55) + coreLine * 0.9) * tail + flare * 1.2) * Opacity;
    float3 color = lerp(MoonBlue, PaleCyan, body);
    color = lerp(color, float3(1.0, 1.0, 1.0), saturate(coreLine + flare * 0.8));
    return float4(color * light, light);
}

technique DarkmoonRay
{
    pass P0 { PixelShader = compile ps_2_0 Ray(); }
}

technique DarkmoonShard
{
    pass P0 { PixelShader = compile ps_2_0 Shard(); }
}

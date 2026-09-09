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
float2 WorldDrawSize;

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

// A projectile-local sibling of NitoReaperSweep/GwynCinderSlash: a thickened band running along
// ONE circle's edge (not two subtracted circles), so it reads as an open "(" bracket instead of a
// crescent MOON pinched to a point at both tips - the previous two-circle-subtraction construction
// had no tuning knob that could fix that, since the pinch is inherent to subtracting two discs.
//
// Also drops GwynCinderSlash/NitoReaperSweep's sweepY/lead01/age reveal mask: that exists to
// animate a SWING growing into frame over Progress, but this is a constant flying projectile with
// no swing to reveal - keeping that mask cropped the arc down to a comet-shaped sliver instead of
// showing the full bracket (confirmed by rendering both in the offline preview harness before
// committing this). The full band is always visible; only the flowing noise texture animates.
float4 ArtoriasSwordSwipePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv * 2.0 - 1.0;

    // Circle center/radius chosen in the offline preview ("tight" candidate) - keeps roughly the
    // old crescent's curvature, just as a uniform-width band instead of a pinched sliver.
    float d = length(p - float2(-0.62, 0.0)) - 1.16;
    // Tapers the band to a point at the very top/bottom of the quad (a blade-like end on each tip).
    float halfWidth = 0.34 * saturate(1.0 - p.y * p.y);

    float2 flowUV = float2(d * 1.30 - Time * 0.55, p.y * 0.55 + Time * 0.10);
    float macro = tex2D(PrimarySampler, flowUV).r;
    float fibers = tex2D(DetailSampler, flowUV * 1.90 + float2(Time * 0.31, -Time * 0.12)).r;

    float lead = saturate((halfWidth - d) * 13.0);
    float tail = saturate((d + halfWidth * (0.85 + macro * 2.30)) * 3.20);
    float body = lead * tail;

    float heat = body * (0.42 + fibers * 0.85);
    float edge = body * saturate((halfWidth * 0.55 - abs(d)) * 6.0);

    float alpha = saturate(body * 1.25 + edge * 0.35) * Opacity;
    float3 material = DarkColor * (body * 0.95)
        + MidColor * (heat * 0.85)
        + CoreColor * (edge * edge * 0.95);
    return float4(vertexColor.rgb * material * Opacity, vertexColor.a * alpha);
}

technique ArtoriasSwordSwipe
{
    pass SwordSwipePass
    {
        PixelShader = compile ps_2_0 ArtoriasSwordSwipePixel();
    }
}

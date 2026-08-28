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

// A projectile-local sibling of NitoReaperSweep: the silhouette is a real side-view bracket,
// while marble flow and fine horizontal fibers supply the abyss material. The hot rim is kept
// narrow so the projectile never collapses back into the old solid-white crescent.
float4 ArtoriasSwordSwipePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv * 2.0 - 1.0;
    p.y *= 1.06;

    float outerDistance = length(p);
    float innerDistance = length(p + float2(0.40, 0.0));
    float outer = saturate((0.96 - outerDistance) * 6.25);
    float cutout = saturate((innerDistance - 0.48) * 6.25);
    float crescent = outer * cutout;

    float2 flowUV = float2(p.y * 0.62 - Time * 0.34, p.x * 0.44 + Time * 0.11);
    float macro = tex2D(PrimarySampler, flowUV).r;
    float fibers = tex2D(DetailSampler,
        float2(p.y * 1.45 - Time * 0.58, p.x * 0.72 + Time * 0.16)).r;
    float churn = saturate(macro * 0.72 + fibers * 0.40 - 0.16);

    float outerRim = saturate((outerDistance - 0.70) * 5.8);
    float innerRim = saturate((0.72 - innerDistance) * 4.2);
    float rim = crescent * saturate(max(outerRim, innerRim * 0.65));
    float tornBody = crescent * saturate(churn + 0.46);
    float hot = rim * saturate(0.30 + churn * 0.78);

    float alpha = saturate(tornBody * 0.78 + rim * 0.62) * Opacity;
    float3 material = DarkColor * (tornBody * 0.88)
        + MidColor * (tornBody * (0.48 + churn * 0.44))
        + CoreColor * (hot * 0.76);
    return float4(vertexColor.rgb * material * Opacity, vertexColor.a * alpha);
}

technique ArtoriasSwordSwipe
{
    pass SwordSwipePass
    {
        PixelShader = compile ps_2_0 ArtoriasSwordSwipePixel();
    }
}

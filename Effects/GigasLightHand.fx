// Gigas grasping light: pixel-art stone slabs with low-frequency moving holy fractures.
// The damage seam is a separate technique so it remains narrow and legible under Reach.
sampler SlabTexture : register(s0);
sampler FlowNoise : register(s1);
sampler CrackNoise : register(s2);

float3 GoldColor;
float3 CoreColor;
float Opacity;
float Time;
float InnerSide;
float2 DrawSize;
float SeamWidth;

float4 GigasLightHandSlabPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 slab = tex2D(SlabTexture, coords);
    float flow = tex2D(FlowNoise, coords * float2(1.35, 2.05) + float2(-Time * 0.045, Time * 0.17)).r;
    float crack = tex2D(CrackNoise, coords * float2(2.65, 3.40) + float2(Time * 0.016, -Time * 0.025)).r;
    float innerFace = saturate((InnerSide * (coords.x - 0.5) + 0.14) * 4.2);
    float fracture = saturate((0.30 - crack) * 4.4) * saturate(flow * 1.42 - 0.27) * slab.a;
    float faceLight = slab.a * innerFace * (0.05 + flow * 0.12);
    float3 stone = slab.rgb * (0.78 + flow * 0.42) * Opacity;
    float3 gold = GoldColor * (fracture * 0.74 + faceLight) * Opacity;
    float3 emission = CoreColor * fracture * flow * 0.13 * Opacity;
    float alpha = slab.a * Opacity;
    return float4(stone + gold + emission, alpha);
}

float4 GigasLightHandSeamPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float x = abs((coords.x - 0.5) * DrawSize.x);
    float flow = tex2D(FlowNoise, coords * float2(1.15, 2.30) + float2(Time * 0.08, -Time * 0.29)).r;
    float crack = tex2D(CrackNoise, coords * float2(2.40, 3.10) + float2(-Time * 0.024, Time * 0.038)).r;
    float coreHalfWidth = SeamWidth * 0.5 + (flow - 0.5) * 4.0;
    float core = saturate((coreHalfWidth - x) / 4.0);
    float halo = saturate((coreHalfWidth + 12.0 - x) / 8.0);
    float agitation = saturate(flow * 0.72 + (1.0 - crack) * 0.34);
    float alpha = saturate(core * (0.72 + agitation * 0.20) + halo * 0.18) * Opacity;
    float3 color = lerp(GoldColor, CoreColor, core * (0.52 + agitation * 0.38));
    float3 emission = CoreColor * core * agitation * 0.18 * Opacity;
    return float4(color * alpha + emission, alpha);
}

technique GigasLightHandSlab
{
    pass GigasLightHandSlabPass { PixelShader = compile ps_2_0 GigasLightHandSlabPixel(); }
}

technique GigasLightHandSeam
{
    pass GigasLightHandSeamPass { PixelShader = compile ps_2_0 GigasLightHandSeamPixel(); }
}

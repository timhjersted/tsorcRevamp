sampler uImage0 : register(s0);

texture noiseTex;
sampler noiseSampler = sampler_state
{
    Texture = (noiseTex);
    AddressU = wrap;
    AddressV = wrap;
};

float active;
float mulColor;
float fadeOut;
float2 texelSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // This pass is also used to build the unlit branch mask.
    if (active == -2)
    {
        return float4(1, 1, 1, 1);
    }

    float center = tex2D(uImage0, coords).r;
    float2 nearOffset = texelSize * 2.0;
    float2 farOffset = texelSize * 6.0;

    float nearGlow = 0;
    nearGlow = max(nearGlow, tex2D(uImage0, coords + float2( nearOffset.x, 0)).r);
    nearGlow = max(nearGlow, tex2D(uImage0, coords + float2(-nearOffset.x, 0)).r);
    nearGlow = max(nearGlow, tex2D(uImage0, coords + float2(0,  nearOffset.y)).r);
    nearGlow = max(nearGlow, tex2D(uImage0, coords + float2(0, -nearOffset.y)).r);

    float farGlow = 0;
    farGlow = max(farGlow, tex2D(uImage0, coords + float2( farOffset.x, 0)).r);
    farGlow = max(farGlow, tex2D(uImage0, coords + float2(-farOffset.x, 0)).r);
    farGlow = max(farGlow, tex2D(uImage0, coords + float2(0,  farOffset.y)).r);
    farGlow = max(farGlow, tex2D(uImage0, coords + float2(0, -farOffset.y)).r);

    float core = smoothstep(0.35, 0.9, center);
    float aura = saturate(nearGlow * 0.52 + farGlow * 0.18);

    // Break up only the aura. The center remains a clean, readable damage line.
    float fracture = pow(1 - tex2D(noiseSampler, coords * 5).r, active == 1 ? 10 : 4);
    aura *= lerp(0.72, 1.12, fracture);

    float strikeStrength = active == 1 ? 1.0 : 0.28;
    float3 outerColor = float3(0.03, 0.18, 0.82);
    float3 innerColor = float3(0.42, 0.78, 1.0);
    float3 coreColor = float3(1.0, 0.97, 0.82);

    float3 color = outerColor * aura * 1.35;
    color += innerColor * max(nearGlow - core * 0.25, 0) * 0.7;
    color += coreColor * core * 1.8;

    float opacity = saturate(max(core, aura)) * strikeStrength * fadeOut * mulColor;
    return float4(color * strikeStrength * fadeOut * mulColor, opacity);
}

technique MarilithLightningLine
{
    pass MarilithLightningLinePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

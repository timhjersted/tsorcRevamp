sampler spriteTexture : register(s0);

float3 glowColor;
float3 coreColor;
float coreAmount;
float opacity;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float sourceAlpha = tex2D(spriteTexture, coords).a * sampleColor.a;
    float alpha = saturate(sourceAlpha * opacity);
    float3 outputColor = lerp(glowColor, coreColor, saturate(coreAmount));
    return float4(outputColor, alpha);
}

technique UnblockableGlow
{
    pass UnblockableGlowPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

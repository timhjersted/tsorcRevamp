sampler spriteTexture : register(s0);

float3 glowColor;
float3 coreColor;
float coreAmount;
float opacity;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float sourceAlpha = tex2D(spriteTexture, coords).a * sampleColor.a;
    float alpha = saturate(sourceAlpha * opacity);
    // NOT saturate(coreAmount): the D3DX effect compiler that produces the .xnb cannot compile a
    // saturate() whose argument has no per-pixel dependency, and reports it as "error compiling
    // expression" at the TECHNIQUE line — which is why this shader silently never had an .xnb and the
    // unblockable aura threw a missing-asset error whenever it fired. fxc accepts it, so it only shows
    // up at .xnb time. Both callers (AttackTelegraphDraw) already pass 0..1, so the clamp is redundant.
    float3 outputColor = lerp(glowColor, coreColor, coreAmount);
    return float4(outputColor, alpha);
}

technique UnblockableGlow
{
    pass UnblockableGlowPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

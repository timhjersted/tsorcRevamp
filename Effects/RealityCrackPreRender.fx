float xOffset;
float yOffset;


float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    return float4(xOffset, yOffset, 0, 1);
}


technique RealityCrackPreRender
{
    pass RealityCrackPreRenderPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
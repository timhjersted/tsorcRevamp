matrix World;
matrix View;
matrix Projection;

struct VSInput
{
    float2 TexCoords : TEXCOORD0;
    float4 Coords : POSITION0;
    float4 Color : COLOR0;
};

struct PSInput
{
    float2 TexCoords : TEXCOORD0;
    float4 Coords : SV_POSITION;
    float4 Color : COLOR0;
};

PSInput BasicVertexShader(in VSInput input)
{
    PSInput output;
    output.Color = input.Color;
    output.TexCoords = input.TexCoords;
    output.Coords = mul(input.Coords, Projection);
    output.Coords = input.Coords;

    return output;
};

float4 BasicPixelShader(PSInput input) : COLOR0
{
    return float4(1,1,1,1);
}

technique FireTrailShader
{
    pass FireTrailShaderPass
    {
        VertexShader = compile vs_2_0 BasicVertexShader();
        PixelShader = compile ps_2_0 BasicPixelShader();
    }
}
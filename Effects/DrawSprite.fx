matrix WorldViewProjection;
texture spriteTexture;
float spriteFramecount;
float spriteCurrentFrame;
float flipSprite;
sampler textureSampler = sampler_state
{
    Texture = (spriteTexture);
    AddressU = wrap;
    AddressV = wrap;
};

struct VertexShaderInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
};

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 textureUV = input.TextureCoordinates;
    textureUV.y /= spriteFramecount;
    textureUV.y += spriteCurrentFrame / spriteFramecount;
    if (flipSprite == -1)
    {
        textureUV.y = 1 - textureUV.y;
    }
    return tex2D(textureSampler, textureUV);
}

technique DrawSprite
{
    pass EffectPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
};
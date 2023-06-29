sampler uImage0 : register(s0);
texture noiseTex;
sampler textureSampler = sampler_state
{
    Texture = (noiseTex);
    AddressU = wrap;
    AddressV = wrap;
};

float2 textureToSizeRatio;
float2 effectSize;
float2 textureSize;
float4 shaderColor;
float falloffdist;
float power;
float active;
float mulColor;
float radius;

//Various constants
const float PI = 3.141592;
const float TWOPI = 6.283184;
const float INVERSETWOPI = 0.1591549;
const float4 empty = float4(0, 0, 0, 0);


const float directions = 16;
const float quality = 3;
const float size = 8;

const float qualityRatio = .3333;
const float dirRatio = 0.196349;


float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 radius = 0.004;
    float4 color = tex2D(uImage0, coords);
    
    
    color += tex2D(uImage0, coords + float2(.7, .7) * radius);
    color += tex2D(uImage0, coords + float2(0, 1) * radius);
    color += tex2D(uImage0, coords + float2(-.7, .7) * radius);
    color += tex2D(uImage0, coords + float2(-.7, -.7) * radius);
    color += tex2D(uImage0, coords + float2(0, -1) * radius);
    color += tex2D(uImage0, coords + float2(.7, -.7) * radius);
    
    radius += 0.004;
    
    color += tex2D(uImage0, coords + float2(.7, .7) * radius);
    color += tex2D(uImage0, coords + float2(0, 1) * radius);
    color += tex2D(uImage0, coords + float2(-.7, .7) * radius);
    color += tex2D(uImage0, coords + float2(-.7, -.7) * radius);
    color += tex2D(uImage0, coords + float2(0, -1) * radius);
    color += tex2D(uImage0, coords + float2(.7, -.7) * radius);
    radius += 0.004;
    
    color += tex2D(uImage0, coords + float2(.7, .7) * radius);
    color += tex2D(uImage0, coords + float2(0, 1) * radius);
    color += tex2D(uImage0, coords + float2(-.7, .7) * radius);
    color += tex2D(uImage0, coords + float2(-.7, -.7) * radius);
    color += tex2D(uImage0, coords + float2(0, -1) * radius);
    color += tex2D(uImage0, coords + float2(.7, -.7) * radius);
    radius += 0.008;
    
    color += tex2D(uImage0, coords + float2(.7, .7) * radius);
    color += tex2D(uImage0, coords + float2(0, 1) * radius);
    color += tex2D(uImage0, coords + float2(-.7, .7) * radius);
    color += tex2D(uImage0, coords + float2(-.7, -.7) * radius);
    color += tex2D(uImage0, coords + float2(0, -1) * radius);
    color += tex2D(uImage0, coords + float2(.7, -.7) * radius);
    
    /*
    for (float i = 0; i < PI; i += dirRatio)
    {
        for (float j = qualityRatio; j <= 1; j += qualityRatio)
        {
            color += tex2D(uImage0, coords + float2(cos(i), sin(i)) * 5 * 1);
        }
    }*/
    
    return color / 34;
}

technique BlurEffect
{
    pass BlurEffectPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
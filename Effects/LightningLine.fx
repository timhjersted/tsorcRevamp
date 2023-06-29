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
float fadeOut;

//Various constants
const float PI = 3.141592;
const float TWOPI = 6.283184;
const float INVERSETWOPI = 0.1591549;
const float4 empty = float4(0, 0, 0, 0);

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 lightningColor = float4(.1, .4, 1, 1);
    if(active == -2)
    {
        return 1;
    }
    
    float intensity = tex2D(uImage0, coords).r;
    
    float lineIntensity = intensity;
    lineIntensity *= 1.0 * active;
    
    float shatterPower = 3;
    if (active == 1)
    {
        shatterPower = 12;
    }
    
    float shatterIntensity = pow(1 - tex2D(textureSampler, coords * 5).r, shatterPower);
    shatterIntensity *= pow(intensity, 2);
    float border = 0.7;
    if (lineIntensity > border)
    {
        lightningColor = lerp(lightningColor, float4(1, 1, 1, 1), (lineIntensity - border) / border);
    }
    
    float finalIntensity = lineIntensity + shatterIntensity;
    
    
    return finalIntensity * lightningColor * fadeOut;
}

technique LightningLine
{
    pass LightningLinePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
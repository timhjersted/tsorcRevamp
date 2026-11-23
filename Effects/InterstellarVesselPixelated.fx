#include "PixelShaderCommon.fxh"

matrix WorldViewProjection;
texture noiseTexture;
sampler textureSampler = sampler_state
{
    Texture = (noiseTexture);
    AddressU = wrap;
    AddressV = wrap;
};

float fadeOut;
float time;
float4 shaderColor;
float4 secondaryColor;
float length;
float speed;
float2 samplePointOffset1;
float2 samplePointOffset2;
float4 PixelGrid;

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
    float2 uv = PixelateShaderUV(input.TextureCoordinates, PixelGrid);
    
    //Calculate how close the current pixel is to the center line of the screen
    float yDistFromCenter = abs(uv.y - 0.5);
    float intensity = 1.0 - yDistFromCenter;
    
    //Raise it to a high exponent, resulting in sharply increased intensity at the center that trails off smoothly
    intensity = intensity * intensity * intensity;
    
    //Make it fade out towards the end
    float start = 0.80;
    float end = 0.15;
    
    //Make it fade in towards the start
    if (uv.x > start)
    {
        intensity = lerp(0.0, intensity, pow(saturate((1.0 - uv.x) / (1 - start)), 0.115));
    }
    if (uv.x < end)
    {
        float endRatio = uv.x / end;
        float endRatioSq = endRatio * endRatio;
        intensity = lerp(0.0, intensity, endRatioSq * endRatioSq);
    }
    // Symmetric y-edge taper (equivalent to uv.y > 0.7 and uv.y < 0.3 cubed).
    float yEdge = saturate((0.5 - yDistFromCenter) * 3.333333);
    intensity *= yEdge * yEdge * yEdge;
    
    float trailProgress = (1 - uv.x * uv.x);
    trailProgress = (trailProgress * 0.33) + 0.66;
    float4 trailColor = lerp(shaderColor * 2, secondaryColor * 5, trailProgress);
    
    float intensitySq = intensity * intensity;
    intensity = intensitySq * intensitySq * intensity * uv.x;
    
    intensity *= fadeOut * fadeOut;
    intensity *= 2;

    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint1 = uv;
    
    samplePoint1.x = (samplePoint1.x) * length / 100;
    samplePoint1.x = samplePoint1.x + time / 2;
    samplePoint1 /= 4;
    
    float2 samplePoint2 = samplePoint1;
    samplePoint1 += samplePointOffset1;
    samplePoint2 += samplePointOffset2;
    samplePoint2.x = samplePoint2.x + time / 2;

    //Get the noise texture at that point
    float sampleIntensity = tex2D(textureSampler, samplePoint1).r;
    sampleIntensity *= tex2D(textureSampler, samplePoint2).r;
    
    float4 noiseColor = float4(1.0, 1.0, 1.0, 1.0);
    noiseColor.r = sampleIntensity * trailColor.r;
    noiseColor.b = sampleIntensity * trailColor.b;
    noiseColor.g = sampleIntensity * trailColor.g;
    
    intensity = saturate(intensity);
    
    return noiseColor * 13.0 * intensity * pow(shaderColor, 2.5);
}


technique InterstellarVessel
{
    pass InterstellarVesselPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
};

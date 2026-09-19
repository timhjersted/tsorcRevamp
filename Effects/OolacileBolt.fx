#include "PixelShaderCommon.fxh"

matrix WorldViewProjection;
texture baseNoise;
sampler baseNoiseSampler = sampler_state
{
    Texture = (baseNoise);
    AddressU = wrap;
    AddressV = wrap;
};
texture secondaryNoise;
sampler secondaryNoiseSampler = sampler_state
{
    Texture = (secondaryNoise);
    AddressU = wrap;
    AddressV = wrap;
};

float fadeOut;
float time;
float4 slashCenter;
float4 slashEdge;
float length;
float speed;
float2 samplePointOffset1;
float2 samplePointOffset2;
float baseNoiseUOffset;
// xy = live 2x2-pixel block count across the trail, zw = its reciprocal.
// OolacileBolt computes this from its current world-space length and full strip width.
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
    // Quantize before all silhouette and noise work so the bolt itself, rather than only
    // its sampled texture, uses a stable 2x2 world-pixel grid as the trail grows.
    float2 uv = PixelateShaderUV(input.TextureCoordinates, PixelGrid);

    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1.0 - abs(uv.y - 0.5);


    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint = uv;

    //Zoom in on the noise texture, then shift it over time to make it appear to be flowing
    samplePoint /= 20;
    samplePoint.x = (samplePoint.x + time * 0.05) * 1;

    //Compress it vertically
    samplePoint.y = samplePoint.y / 2;

    //Get the noise texture at that point
    float sampleIntensity = tex2D(baseNoiseSampler, samplePoint).r;

    //Raise it to a high exponent, resulting in sharply increased intensity at the center that trails off smoothly
    //Higher number = more narrow and compressed trail
    intensity = pow(intensity, 6.0) * uv.x;


    //Make it fade out towards all 4 edges
    float start = 0.8;
    float end = 0.01 + (1 - fadeOut);
    float yStart = 0.9;
    float yEnd = 0.1;

    float frontFade = saturate((1.0 - uv.x) / (1.0 - start));
    intensity *= frontFade * frontFade;

    float backFade = saturate(uv.x / end);
    float backFade2 = backFade * backFade;
    intensity *= backFade2 * backFade2;

    float upperFade = saturate((1.0 - uv.y) / yEnd);
    float upperFade2 = upperFade * upperFade;
    intensity *= upperFade2 * upperFade2 * upperFade;

    float lowerFade = saturate(uv.y / yEnd);
    float lowerFade2 = lowerFade * lowerFade;
    intensity *= lowerFade2 * lowerFade2 * lowerFade;


    //Yet again tune the curve
    intensity = pow(intensity, 1.75);

    //Make it fade out toward the far edge
    intensity = intensity * uv.x;

    float2 baseNoiseVector = float2((uv.x / 5) + time / 5, uv.y);
    float baseNoiseIntensity = tex2D(baseNoiseSampler, baseNoiseVector).r;
    intensity *= baseNoiseIntensity * baseNoiseIntensity * baseNoiseIntensity * 10;

    float4 trailColor = slashCenter;
    if (intensity < 0.9)
    {
        trailColor = slashEdge;
    }

    //TODO: Replace this with actual tonemapping
    if (intensity > 1)
    {
        //intensity = 1;
    }

    // Includes the legacy center-line multiplier of 3 folded out of the earlier shape math.
    float outputIntensity = 18.0 * intensity * sampleIntensity * fadeOut * fadeOut;
    return trailColor * outputIntensity;
}

technique OolacileBolt
{
    pass EffectPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
};

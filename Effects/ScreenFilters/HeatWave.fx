sampler uImage0 : register(s0);
float3 uColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float uOpacity;
float uTime;
float uProgress;

float uSaturation;
float uIntensity;
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uSecondaryColor;
float2 uDirection;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float4 uSourceRect;
float2 uZoom;

const float INVERSETWOPI = 0.1591549;

float4 PixelShaderFunction(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{    
    //Offset and scale it
    float2 currentPixel = coords * uScreenResolution;
    float2 focusPoint = uTargetPosition - uScreenPosition;
    float2 drawPosition = (currentPixel - focusPoint) / uImageSize1;
    //float4 outColor = tex2D(uImage0, coords);
    //float dist = 2 * distance(currentPixel, focusPoint) / uScreenResolution.x;
    
    float angle = atan2(drawPosition.y, drawPosition.x);
    float pixDist = distance(coords, float2(0.5, 0.5));
    float dist = 2 * distance(currentPixel, focusPoint) / uScreenResolution.x;
    float2 samplePoint = (currentPixel / uImageSize1) / 10;
    samplePoint.y += uTime / 10;
    float2 distortOffset = tex2D(uImage1, samplePoint).rg - float2(0.5, 0.5);
    
    
    distortOffset *= pow(dist, 2 * uOpacity) * uIntensity * 0.5; // Make it get more intense toward targetPosition
    
    if (dist > 6)
    {
        distortOffset = 0;
    }
    
    return tex2D(uImage0, coords + distortOffset);
}


technique HeatWave
{
    pass HeatWavePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
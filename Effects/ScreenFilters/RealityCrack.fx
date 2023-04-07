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

const float PI = 3.141592;
const float SQRTTwoOverTwo = 0.707107;
float4 PixelShaderFunction(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    //Various Constants
    float distortionStrength = 0.05f;
    float abberationStrength = 1.1;
    float ringSize = 300;
    float PIOver4 = PI / 4;
    
    //Offset and scale it
    float2 currentPixel = coords * uScreenResolution;
    float2 focusPoint = uTargetPosition - uScreenPosition;
    float2 drawPosition = (currentPixel - focusPoint) / uImageSize1;
    float4 outColor = tex2D(uImage0, coords);
    float2 distortion = float2(0, 0);
    
    if (drawPosition.x > 0 && drawPosition.x < 1 &&
        drawPosition.y > 0 && drawPosition.y < 1)
    {
        distortion = uDirection * tex2D(uImage1, drawPosition).b / 2;        
    }
    return tex2D(uImage0, coords + distortion);
}


technique RealityCrack
{
    pass RealityCrackPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
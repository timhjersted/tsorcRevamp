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
    
    //Calculate the distance from the current pixel to the focus point of the effect
    float dist = 2 * distance(currentPixel, focusPoint) / uScreenResolution.x;
    float2 diff = focusPoint - currentPixel;
    
    //Get rotated idiot
    //Simplified equation for rotating a vector2 by 45 degrees, since there is no Vector2.RotatedBy() here
    float2 rotatedDiff = float2(SQRTTwoOverTwo * (diff.x - diff.y), SQRTTwoOverTwo * (diff.x + diff.y));    
        
    //How intense the distortion effect should be. If this is > 1.8 it is shaded in solid black to make the 'core' of the effect.
    float intensity = pow(rotatedDiff.x, 1.0 / 3.0) + pow(rotatedDiff.y, 1.0 / 3.0);
    
    //Controls how big it is. uProgress goes from 0 to 1 normally
    float scaleFactor = lerp(0, 40, uProgress);
    
    //Scale it
    intensity = scaleFactor / pow(intensity, 1.5);
    
    
    float modTime = 1 - frac(uTime / 5);
    modTime = uProgress + 1;

    //Calculate the current pixels distance to the edge of the circle
    float ringDistance = pow(1 - abs(modTime - dist), 5);
    ringDistance = intensity;
    
    float distanceFactor = 0;
    //distanceFactor = dist;

    //Make it fall off with distance at a different rate in vs outside the circle
    if (modTime > intensity)
    {
        distanceFactor = pow(abs(ringDistance), 6.0);

    }
    else
    {
        distanceFactor = pow(abs(ringDistance), 8.0);
    }
    
    float2 normalDiff = normalize(currentPixel - focusPoint) * distortionStrength * distanceFactor * 20 * uOpacity;
    
    float4 pixelColor = float4(1, 1, 1, 1);
    pixelColor.r = tex2D(uImage0, coords + normalDiff / abberationStrength).r;
    pixelColor.g = tex2D(uImage0, coords + normalDiff).g;
    pixelColor.b = tex2D(uImage0, coords + normalDiff * abberationStrength).b;
    
    if (ringDistance < 0)
    {
        ringDistance = 0;
    }
    float3 color = float3(0, 0, 1);
    
    if (uColor.x != 1 || uColor.y != 1 || uColor.z != 1)
    {
        color = uColor;
    }
    float adjustedOpacity = uOpacity;
    if (adjustedOpacity > 0)
    {
        adjustedOpacity = 0;
    }
    pixelColor.rgb += (color.rgb * (ringDistance * ringDistance / 6)) * adjustedOpacity;
    
    
    if (intensity > 1.8)
    {
        return 0;
    }
    else if (uColor.r > 0)
    {

        return lerp(float4(uColor.r, uColor.r, uColor.r, uColor.r), pixelColor, 1 - uColor.r);
    }
    else
    {        
        return pixelColor;
    }
}


technique RealityCrack
{
    pass RealityCrackPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
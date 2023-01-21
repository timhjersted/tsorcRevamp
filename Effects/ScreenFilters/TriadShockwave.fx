sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

float4 PixelShaderFunction(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    //Various Constants
    float distortionStrength = 0.05f;
    float abberationStrength = 2.1;
    float ringSize = 300;
    
    //Important variables
    float4 pixelColor = float4(1, 1, 1, 1);
    float2 currentPixel = coords * uScreenResolution;    
    float2 focusPoint = uTargetPosition - uScreenPosition;
    float baseRadius = 0.1;
    if (uIntensity != 0)
    {
        baseRadius = uIntensity;
    }
    
    //Calculate the distance from the current pixel to the focus point
    float dist = 2 * distance(currentPixel, focusPoint) / uScreenResolution.x;
   
    float modTime = 1 - frac(uTime / 5);
    modTime = uProgress + baseRadius;


    //Calculate the current pixels distance to the edge of the circle
    float ringDistance = pow(1 - abs(modTime - dist), 5);
    if (ringDistance > 1 || ringDistance < 0)
    {
        ringDistance = 0;
    }
    float distanceFactor = 0;
    //distanceFactor = dist;

    //Make it fall off with distance at a different rate in vs outside the circle
    if (modTime > dist)
    {
        distanceFactor = pow(abs(ringDistance), 6.0);

    }
    else
    {
        distanceFactor = pow(abs(ringDistance), 8.0);
    }
    
    float2 normalDiff = normalize(currentPixel - focusPoint) * distortionStrength * distanceFactor * 2 * uOpacity;
    
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
    return pixelColor;
}


technique TriadShockwave
{
    pass TriadShockwavePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}


    
    /*
    float progress = 1.0 - ((radius - dist) / ringSize);
    progress = 1 - (abs(radius - dist) / ringSize);
    if (dist > radius)
    {
        progress = pow(progress, 15);
    }
    else if(dist >  radius / 2)
    {
        progress = pow(progress, 5);        
    }
    else
    {
        progress = 0;
    }
    //return progress;
    
    if (dist < radius)
    {
        progress = progress * sin(progress * 20.0);
    }*/
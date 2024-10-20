sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;



float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{

    //Controls the radius
    float modTime = 0.05 * uSaturation;

    //The center of the draw area, used to simplify calculations
    float2 fragCenter = float2(0.5, 0.5);

    //Calculate how far the current pixel is from the center
    float dist = distance(coords, fragCenter) * 2;

    //Make the fire more intense close to the edge of the radius, tapering off with distance
    float distanceFactor = (1.0f - (modTime - dist));

    if (modTime > dist) {
        //Make its intensity taper off slowly the further inside the radius
        distanceFactor = pow(abs(distanceFactor), 500.0);
    }
    else
    {
        //And taper off faster the further outside it
        distanceFactor = 1.0 + (modTime - dist);
        distanceFactor = pow(abs(distanceFactor), 30.0);
    }

    //Make it offset as the npc moves in the world (the npcs center coordinates are fed to uColor.xy)
    float2 offsetCoords = float2(frac(uColor.x / 1), frac(uColor.y / 1));
    offsetCoords.x = uColor.x / 700;
    offsetCoords.y = uColor.y / 700;

    float2 fogOffset = float2(uColor.z, uOpacity);
    fogOffset /= 1000;

    coords *= 4;
    //Calculate how intense a pixel should be based on the noise
    float intensity = tex2D(uImage0, frac(coords + offsetCoords)).r * sampleColor;
    float intensity2 = tex2D(uImage0, frac(coords + fogOffset)).r * sampleColor;
    
    //Calculate and output the final color of the pixel    
    distanceFactor = distanceFactor * 2.5f;
    float r = pow(intensity, 8.0 / 5.0) * 1.5f * pow(distanceFactor, 1.5);
    float g = pow(intensity, 6.0 / 5.0) * .5f * distanceFactor * distanceFactor;
    float b = pow(intensity, 4.0 / 5.0) * 1.45f * distanceFactor * distanceFactor;
    float a = 0;
    
    if (modTime < dist) {
        
        r += pow(intensity2, 8) * 1.5f;
        g += pow(intensity2, 6.0) * .5f;
        b += pow(intensity2, 3.0) * 0.65f;
        a = 1;
    }
    
    return float4(r, g, b, a) *  sampleColor * uSaturation;
}

technique AttraidiesInverseAura
{
    pass AttraidiesInverseAuraPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
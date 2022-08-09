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
    float modTime = 1;
    float timeFactor = 1;
    float progress = frac(uSaturation / 3 / 60) * 3;

    //Slowly expand for the first second
    if (progress <= 2) {
        modTime = (1 / 40) - ((1 / 40) * (1 / (progress + 1)));
        timeFactor = 2 - progress;
    }
    if (progress > 2) {
        timeFactor = 0;
    }

    //The center of the draw area, used to simplify calculations
    float2 fragCenter = float2(0.5, 0.5);

    //Calculate how far the current pixel is from the center
    float dist = distance(coords, fragCenter) * 2;

    //Make the fire more intense close to the edge of the radius, tapering off with distance
    float distanceFactor = (1.0f - (modTime - dist));

    if (modTime > dist) {
        //Make its intensity taper off slowly the further inside the radius
        distanceFactor = pow(abs(distanceFactor), 45.0);
    }
    else
    {
        //And taper off faster the further outside it
        distanceFactor = 1.0 + (modTime - dist);
        distanceFactor = pow(abs(distanceFactor), 290.0);
    }

    //Make it offset as the npc moves in the world (the npcs center coordinates are fed to uColor.xy)
    float2 offsetCoords = float2(frac(uColor.x / 1), frac(uColor.y / 1));
    offsetCoords.x = uColor.x / 12000;
    offsetCoords.y = uColor.y / 12000;

    //Calculate how intense a pixel should be based on the noise
    float intensity = tex2D(uImage0, frac(coords + offsetCoords)).r * sampleColor;
    intensity = pow(intensity, 0.2);

    //Calculate and output the final color of the pixel    
    distanceFactor = distanceFactor * 2.5f;
    float r = pow(intensity, 8) * 1.5f * pow(distanceFactor, 1.5);
    float g = pow(intensity, 6.0) * .5f * distanceFactor * distanceFactor;
    float b = pow(intensity, 3.0) * 0.15f * distanceFactor * distanceFactor;

    return float4(r , g , b , 1) * sampleColor * timeFactor;
}

technique MarilithIntro
{
    pass MarilithIntroPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
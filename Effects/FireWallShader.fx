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
    //Calculate the radius based on the time
    //Ingame the radius will be controlled by an external variable instead
    //float modTime = frac(uTime / 10) * 1.0;

    //Make the fire more intense close to the edge of the radius, tapering off with distance
    float distanceFactor = 0;
    float textureSize = 1024;
    float projectileWidth = 140;
    
        
    //float2 pixelatedCoord = coords;
    //float pixelSize = 0.001;
    //pixelatedCoord.x = coords.x - fmod(coords.x, pixelSize);
    //pixelatedCoord.y = coords.y - fmod(coords.y, pixelSize);
    
    //coords = pixelatedCoord;
    //return float4(1, 1, 1, 1) * coords.x;

    //distanceFactor = pow(abs(distanceFactor), 45.0);
    float2 samplePos = float2(0, 0);
    float timeFactor = uTime / 7;
    
    float2 heightNoise;    
    heightNoise.x = timeFactor * .25;
    heightNoise.y = coords.y * 0.25;
    float edgeFade = 1;
    
    //Left
    if (uSaturation == 0) {
        samplePos.x = -1;
        distanceFactor = 1.2 - (coords.x * textureSize / projectileWidth);
    }
    //Right
    if (uSaturation == 1) {
        samplePos.x = 1;
        distanceFactor = coords.x * textureSize / projectileWidth + .2;
    }
    
    //Top (evil)
    float cloudProgress = 0;
    if (uSaturation == 2 || uSaturation == 4)
    {
        timeFactor = uSecondaryColor.z / 500;
        heightNoise.y = timeFactor * .25;
        heightNoise.x = coords.x * 0.25;

        distanceFactor = 1.1 - (coords.y * textureSize / projectileWidth);
        samplePos.y = -1;


        if (uSecondaryColor.y > 0) {
            cloudProgress = uSecondaryColor.y / 300;
        }


        //distanceFactor += cloudProgress;
        if (distanceFactor > 1) {
            distanceFactor = 1;
        }
    }

    //Bottom
    if (uSaturation == 3) {
        samplePos.y = 1;
        distanceFactor = coords.y * textureSize / projectileWidth + .2;
        heightNoise.y = timeFactor * .25;
        heightNoise.x = coords.x * 0.25;
    }
    
    
    samplePos *= frac(timeFactor);
    samplePos += coords;
    samplePos = frac(samplePos);

    //distanceFactor *= 60;
    //Calculate how intense a pixel should be based on the noise generator
    
    float intensity = tex2D(uImage0, samplePos).r;
    intensity = pow(intensity, 1.8);
    
    float flameHeight = tex2D(uImage0, heightNoise).r;
    distanceFactor = 3 * pow(distanceFactor, 1 / pow(flameHeight, 2));    

    float fadeFactor;
    float fadeLimit;
    if (uSaturation >= 2)
    {
        fadeLimit = 4.087;
        fadeFactor = coords.x;
    }
    else
    {
        fadeLimit = 1.805;
        fadeFactor = coords.y;
    }
    
    if (fadeFactor < .1)
    {
        distanceFactor *= pow(fadeFactor / .1, 9);
    }
    
    if (fadeFactor > fadeLimit)
    {
        distanceFactor *= pow(1 - (fadeFactor - fadeLimit) / .1, 9);
    }
    
    //Calculate and output the final color of the pixel    
    //distanceFactor = distanceFactor * 2.5f;
    //return float4(distanceFactor, intensity, 1, 0.5);

    float r = 2 * pow(intensity, 1) * .5f * pow(distanceFactor, 2);
    float g = 2 * pow(intensity, 2.0) * 0.2 * pow(distanceFactor, 3);
    float b = 2 * pow(intensity, 3.0) * 1.15f * pow(distanceFactor, 3);

    float3 final = lerp(float3(r, g, b), float3(1, 1, 1) * pow(intensity, 1.5) * distanceFactor * distanceFactor, cloudProgress);

    return float4(final, 1) * 2 * (uSecondaryColor.x / 100.0);
}

technique FireWallShader
{
    pass FireWallShaderPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
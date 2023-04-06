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


    //float uTime = 0.5;
    float pixelSize = 0.001;
    //Calculate the radius based on the time
    //Ingame the radius will be controlled by an external variable instead
    float modTime = frac(uTime / 10) * 1.0;
    modTime = uSaturation;
    //float modTime = uTime;
    //float radius = modTime;
    //float2 fragCenter = uImageSize0.xy / 2.0;
    float2 fragCenter = float2(0.5, 0.5);

    //Pixelation effect, achieved via fucking with the coordinates. Commented out right now because it nukes the detail on the noise map from orbit.
    float2 pixelatedCoord = coords;
    //pixelatedCoord.x = coords.x - fmod(coords.x, pixelSize);
    //pixelatedCoord.y = coords.y - fmod(coords.y, pixelSize);

    //Calculate how far the current pixel is from the center
    float dist = distance(pixelatedCoord, fragCenter) * 2;


    //Make the fire more intense close to the edge of the radius, tapering off with distance
    float distanceFactor = (1.0f - (modTime - dist));


    //return float4(dist * dist, dist * dist, dist * dist, 1);

    if (modTime > dist) {
        //Make its intensity taper off slow when inside the radius
        distanceFactor = pow(abs(distanceFactor), 290.0);

    }
    else
    {
        //And taper off faster when outside it
        distanceFactor = 1.0 + (modTime - dist);
        distanceFactor = pow(abs(distanceFactor), 45.0);

        //distanceFactor = 1;
    }


    //float2 uv = coords.xy / uResolution;

    //Calculate how intense a pixel should be based on the noise generator
    float intensity = tex2D(uImage0, pixelatedCoord * 1).r * sampleColor;
    intensity = pow(intensity, 0.2);
    //intensity -= 0.3;
    //intensity /= 0.7;
    //return float4(intensity, intensity, intensity, 1);

    //float intensity = 1;
    //Calculate and output the final color of the pixel    
    distanceFactor = distanceFactor * 2.5f;
    //return float4(distanceFactor, intensity, 1, 0.5);

    if (modTime > 1.5) {
        distanceFactor /= 5 * (modTime - 0.5);
    }

    float r = pow(intensity, 5) * 1.5f * pow(distanceFactor, 1.5);
    float g = pow(intensity, 7.0) * .5f * pow(distanceFactor, 3);
    float b = pow(intensity, 2.0) * 0.04f * pow(distanceFactor, 4);
    return float4(r , g , b , 1) * sampleColor * uOpacity;
}

technique FireWaveShader
{
    pass SolarDetonationShaderPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}